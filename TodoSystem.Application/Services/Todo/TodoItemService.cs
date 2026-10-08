using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Application.Services.Todo;

public class TodoItemService(TodoSystemDbContext db) : ITodoItemService
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;

    public async Task<IReadOnlyList<TodoItemDto>> ListAsync(
        int userId,
        TodoItemQuery query,
        CancellationToken ct = default)
    {
        var items = db.TodoItems
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (query.ListId is { } listId)
        {
            items = items.Where(x => x.ListId == listId);
        }

        if (query.Unassigned == true)
        {
            items = items.Where(x => x.ListId == null);
        }

        if (query.ParentId is { } parentId)
        {
            items = items.Where(x => x.ParentId == parentId);
        }

        if (query.TagId is { } tagId)
        {
            items = items.Where(x => x.ItemTags.Any(link => link.TagId == tagId));
        }

        if (query.Statuses is { Length: > 0 } statuses)
        {
            // 逐个校验，非法取值直接 400，而不是静默返回空列表 ——
            // 「参数写错了」和「确实没有数据」必须能区分开。
            var normalized = statuses.Select(TodoValidation.RequireStatus).ToArray();

            items = items.Where(x => normalized.Contains(x.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            var priority = TodoValidation.RequirePriority(query.Priority);

            items = items.Where(x => x.Priority == priority);
        }

        if (query.DueBefore is { } dueBefore)
        {
            items = items.Where(x => x.DueAt != null && x.DueAt <= dueBefore);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = LikePattern(query.Keyword);

            items = items.Where(x => EF.Functions.ILike(x.Title, pattern, "\\"));
        }

        // 排序必须发生在 Project 之前：写在之后的话 EF 会把 OrderBy 折进 Select，
        // 变成对 new TodoItemDto(...).SortOrder 排序，那是翻译不了的表达式。
        return await Project(
                items.OrderBy(x => x.SortOrder).ThenBy(x => x.Id),
                DateTimeOffset.UtcNow)
            .ToListAsync(ct);
    }

    public async Task<TodoItemDetailDto> GetAsync(
        int userId,
        int itemId,
        CancellationToken ct = default)
    {
        // 同一次请求内固定一个 now，否则详情和子任务的 IsOverdue 可能落在不同的时间点上。
        var now = DateTimeOffset.UtcNow;

        var item = await Project(db.TodoItems.AsNoTracking().Where(Owned(userId, itemId)), now)
            .FirstOrDefaultAsync(ct)
            ?? throw new CustomException(404, "待办不存在");

        var children = await Project(
                db.TodoItems.AsNoTracking()
                    .Where(x => x.ParentId == itemId && x.UserId == userId)
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id),
                now)
            .ToListAsync(ct);

        return new TodoItemDetailDto(item, children);
    }

    public async Task<TodoItemDto> CreateAsync(
        int userId,
        CreateTodoItemRequest request,
        CancellationToken ct = default)
    {
        var title = TodoValidation.RequireText(request.Title, "标题", MaxTitleLength);
        var status = TodoValidation.RequireStatus(request.Status ?? TodoStatuses.Todo);
        var priority = TodoValidation.RequirePriority(request.Priority ?? TodoPriorities.Normal);

        if (request.ListId is { } listId)
        {
            await EnsureListOwnedAsync(userId, listId, ct);
        }

        if (request.ParentId is { } parentId)
        {
            await EnsureParentUsableAsync(userId, parentId, ct);
        }

        var item = new TodoItemEntity
        {
            UserId = userId,
            ListId = request.ListId,
            ParentId = request.ParentId,
            Title = title,
            Description = TodoValidation.OptionalText(request.Description, "说明", MaxDescriptionLength),
            Priority = priority,
            DueAt = request.DueAt,
            SortOrder = request.SortOrder ?? 0,
        };

        // 必须在这里就把 completed_at 和 status 对齐：数据库的
        // ck_todo_items_completed_at_consistent 不允许「DONE 但没有完成时间」的行落库。
        ApplyStatus(item, status, DateTimeOffset.UtcNow);

        db.TodoItems.Add(item);
        await db.SaveChangesAsync(ct);

        return await GetFlatAsync(userId, item.Id, ct);
    }

    public async Task<TodoItemDto> UpdateAsync(
        int userId,
        int itemId,
        UpdateTodoItemRequest request,
        CancellationToken ct = default)
    {
        var item = await FindAsync(userId, itemId, ct);

        var title = TodoValidation.RequireText(request.Title, "标题", MaxTitleLength);
        var status = TodoValidation.RequireStatus(request.Status);
        var priority = TodoValidation.RequirePriority(request.Priority);

        if (request.ListId is { } listId)
        {
            await EnsureListOwnedAsync(userId, listId, ct);
        }

        item.Title = title;
        item.Description = TodoValidation.OptionalText(request.Description, "说明", MaxDescriptionLength);
        item.ListId = request.ListId;
        item.Priority = priority;
        item.DueAt = request.DueAt;
        item.SortOrder = request.SortOrder ?? item.SortOrder;

        ApplyStatus(item, status, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(ct);

        return await GetFlatAsync(userId, itemId, ct);
    }

    public async Task<TodoItemDto> ChangeStatusAsync(
        int userId,
        int itemId,
        string status,
        CancellationToken ct = default)
    {
        var normalized = TodoValidation.RequireStatus(status);
        var item = await FindAsync(userId, itemId, ct);

        ApplyStatus(item, normalized, DateTimeOffset.UtcNow);

        await db.SaveChangesAsync(ct);

        return await GetFlatAsync(userId, itemId, ct);
    }

    public async Task DeleteAsync(int userId, int itemId, CancellationToken ct = default)
    {
        var item = await FindAsync(userId, itemId, ct);

        // 子任务跟着父任务一起软删除。只删父任务会留下「父任务不在了、子任务还挂在列表上」
        // 的孤儿行 —— 既无法展示为子任务，又不会消失。
        var children = await db.TodoItems
            .Where(x => x.ParentId == itemId && x.UserId == userId)
            .ToListAsync(ct);

        db.TodoItems.RemoveRange(children);
        db.TodoItems.Remove(item);

        // 标签关联行刻意保留：待办是软删除，将来恢复时标签还在。
        // 关联行本身很小，且不会出现在任何查询结果里（待办已被过滤）。
        await db.SaveChangesAsync(ct);
    }

    public async Task<TodoItemDto> AttachTagsAsync(
        int userId,
        int itemId,
        IReadOnlyList<int> tagIds,
        CancellationToken ct = default)
    {
        await FindAsync(userId, itemId, ct);

        var wanted = tagIds.Distinct().ToList();

        if (wanted.Count == 0)
        {
            return await GetFlatAsync(userId, itemId, ct);
        }

        // 归属校验：标签必须全是这个用户的。
        // 少了这一步，A 就能把自己的待办打上 B 的标签，进而在按标签筛选时看到 B 的标签名。
        var owned = await db.Tags
            .Where(x => x.UserId == userId && wanted.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var missing = wanted.Except(owned).ToList();

        if (missing.Count > 0)
        {
            throw new CustomException(400, $"标签不存在或不属于当前用户：{string.Join(", ", missing)}");
        }

        // 已挂上的跳过，让整个操作可以安全重试
        var linked = await db.TodoItemTags
            .Where(x => x.TodoItemId == itemId && wanted.Contains(x.TagId))
            .Select(x => x.TagId)
            .ToListAsync(ct);

        foreach (var tagId in wanted.Except(linked))
        {
            db.TodoItemTags.Add(new TodoItemTagEntity
            {
                TodoItemId = itemId,
                TagId = tagId,
            });
        }

        await db.SaveChangesAsync(ct);

        return await GetFlatAsync(userId, itemId, ct);
    }

    public async Task<TodoItemDto> DetachTagAsync(
        int userId,
        int itemId,
        int tagId,
        CancellationToken ct = default)
    {
        await FindAsync(userId, itemId, ct);

        var link = await db.TodoItemTags
            .FirstOrDefaultAsync(x => x.TodoItemId == itemId && x.TagId == tagId, ct);

        // 没挂过也算成功：摘标签是幂等的，客户端重试不该报错
        if (link is not null)
        {
            db.TodoItemTags.Remove(link);
            await db.SaveChangesAsync(ct);
        }

        return await GetFlatAsync(userId, itemId, ct);
    }

    /// <summary>
    /// 把 status 和 completed_at 一起改到位，两者必须同步。
    /// 已经是 DONE 再置 DONE 时保留原有的完成时间，否则重复点击「完成」会刷新完成时刻，统计失真。
    /// </summary>
    private static void ApplyStatus(TodoItemEntity item, string status, DateTimeOffset now)
    {
        item.Status = status;

        item.CompletedAt = status == TodoStatuses.Done
            ? item.CompletedAt ?? now
            : null;
    }

    /// <summary>查询条件：主键 + 归属，两者必须同时成立。</summary>
    private static Expression<Func<TodoItemEntity, bool>> Owned(int userId, int itemId)
        => x => x.Id == itemId && x.UserId == userId;

    /// <summary>找不到或不属于该用户都返回 404，理由见 <see cref="TodoListService"/>。</summary>
    private async Task<TodoItemEntity> FindAsync(int userId, int itemId, CancellationToken ct)
        => await db.TodoItems.FirstOrDefaultAsync(Owned(userId, itemId), ct)
           ?? throw new CustomException(404, "待办不存在");

    private async Task<TodoItemDto> GetFlatAsync(int userId, int itemId, CancellationToken ct)
    {
        return await Project(db.TodoItems.AsNoTracking().Where(Owned(userId, itemId)), DateTimeOffset.UtcNow)
            .FirstOrDefaultAsync(ct)
            ?? throw new CustomException(404, "待办不存在");
    }

    /// <summary>
    /// 清单必须存在且属于该用户。这里给 400 而不是依赖数据库的复合外键报错：
    /// 外键确实能拦住越权写入，但它抛的是 23503，落到接口层就是一个 500。
    /// 外键是最后一道防线，不是报错文案的来源。
    /// </summary>
    private async Task EnsureListOwnedAsync(int userId, int listId, CancellationToken ct)
    {
        var exists = await db.TodoLists
            .AnyAsync(x => x.Id == listId && x.UserId == userId, ct);

        if (!exists)
        {
            throw new CustomException(400, "指定的清单不存在或不属于当前用户");
        }
    }

    /// <summary>
    /// 父任务必须存在、属于该用户，且本身是顶层任务。
    /// 「最多两层」这条规则数据库表达不了（自引用外键管不到深度），只能在这里守 ——
    /// 不守的后果是无限嵌套，展示时要么递归爆栈，要么只能砍掉。
    /// </summary>
    private async Task EnsureParentUsableAsync(int userId, int parentId, CancellationToken ct)
    {
        var parent = await db.TodoItems
            .Where(x => x.Id == parentId && x.UserId == userId)
            .Select(x => new { x.ParentId })
            .FirstOrDefaultAsync(ct)
            ?? throw new CustomException(400, "指定的父任务不存在或不属于当前用户");

        if (parent.ParentId is not null)
        {
            throw new CustomException(400, "子任务下不能再建子任务，层级最多两层");
        }
    }

    /// <summary>
    /// 关键字转 LIKE 模式，并把用户输入里的通配符转义掉。
    /// 不转义的话搜「50%」会变成「以 50 开头」，搜「a_b」会连「axb」一起匹配出来。
    /// 转义符用反斜杠，与 ILike 的第三个参数对应。
    /// </summary>
    private static string LikePattern(string keyword)
    {
        var escaped = keyword.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");

        return $"%{escaped}%";
    }

    private IQueryable<TodoItemDto> Project(IQueryable<TodoItemEntity> query, DateTimeOffset now)
        => query.Select(x => new TodoItemDto(
            x.Id,
            x.ListId,
            x.List != null ? x.List.Name : null,
            x.ParentId,
            x.Title,
            x.Description,
            x.Status,
            x.Priority,
            x.DueAt,
            x.CompletedAt,
            x.SortOrder,
            // 逾期不能用实体的 IsOverdue（[NotMapped]），它翻译不成 SQL。
            // 把 now 作为参数传进来，这里就是一次普通的列比较。
            x.CompletedAt == null && x.DueAt != null && x.DueAt < now,
            x.Children.Count(),
            x.Children.Count(child => child.Status == TodoStatuses.Done),
            // 标签从 db.Tags 侧查，而不是 x.ItemTags.Select(t => t.Tag!...)：后者会 LEFT JOIN
            // 出已软删除的标签行、tag 列全为 NULL，再把 NULL 塞进 TagDto 的非空 int Id 就会在物化时炸掉。
            db.Tags
                .Where(tag => tag.ItemTags.Any(link => link.TodoItemId == x.Id))
                .OrderBy(tag => tag.Name)
                .Select(tag => new TagDto(tag.Id, tag.Name, tag.Color))
                .ToList(),
            x.CreatedAt,
            x.UpdatedAt));
}
