using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoSystem.Application.Common;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Application.Services.Todo;

public class TodoListService(TodoSystemDbContext db) : ITodoListService
{
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;
    private const int MaxColorLength = 20;

    public async Task<IReadOnlyList<TodoListDto>> ListAsync(
        int userId,
        bool includeArchived,
        CancellationToken ct = default)
    {
        var query = db.TodoLists
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        // 排序必须在投影之前。写在 Project(...) 之后的话 EF 会把 OrderBy 折进 Select，
        // 变成对 new TodoListDto(...).SortOrder 排序，那是个无法翻译的表达式。
        return await Project(query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id))
            .ToListAsync(ct);
    }

    public async Task<TodoListDto> GetAsync(
        int userId,
        int listId,
        CancellationToken ct = default)
    {
        return await Project(db.TodoLists.AsNoTracking().Where(Owned(userId, listId)))
            .FirstOrDefaultAsync(ct)
            ?? throw new CustomException(404, "清单不存在");
    }

    public async Task<TodoListDto> CreateAsync(
        int userId,
        CreateTodoListRequest request,
        CancellationToken ct = default)
    {
        var name = TodoValidation.RequireText(request.Name, "清单名称", MaxNameLength);

        var list = new TodoListEntity
        {
            UserId = userId,
            Name = name,
            Description = TodoValidation.OptionalText(request.Description, "清单说明", MaxDescriptionLength),
            Color = TodoValidation.OptionalText(request.Color, "颜色", MaxColorLength),
            SortOrder = request.SortOrder ?? 0,
        };

        db.TodoLists.Add(list);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // 并发创建同名清单会撞上部分唯一索引 ix_todo_lists_user_id_name，
            // 把 23505 翻译成 409，避免泄漏成 500。
            db.Entry(list).State = EntityState.Detached;
            throw new CustomException(409, $"已存在名为「{name}」的清单");
        }

        return await GetAsync(userId, list.Id, ct);
    }

    public async Task<TodoListDto> UpdateAsync(
        int userId,
        int listId,
        UpdateTodoListRequest request,
        CancellationToken ct = default)
    {
        var list = await FindAsync(userId, listId, ct);
        var name = TodoValidation.RequireText(request.Name, "清单名称", MaxNameLength);

        list.Name = name;
        list.Description = TodoValidation.OptionalText(request.Description, "清单说明", MaxDescriptionLength);
        list.Color = TodoValidation.OptionalText(request.Color, "颜色", MaxColorLength);
        list.SortOrder = request.SortOrder ?? list.SortOrder;
        list.IsArchived = request.IsArchived;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new CustomException(409, $"已存在名为「{name}」的清单");
        }

        return await GetAsync(userId, listId, ct);
    }

    public async Task DeleteAsync(
        int userId,
        int listId,
        CancellationToken ct = default)
    {
        var list = await FindAsync(userId, listId, ct);

        // 先把清单里的待办退回收件箱，再软删除清单。
        //
        // 这一步不能指望外键：todo_items 用的是复合外键 (user_id, list_id)，没法配
        // ON DELETE SET NULL —— SET NULL 会把 user_id 也置空，而它是 NOT NULL。
        //
        // ExecuteUpdateAsync 走 SQL 直更、绕过 ChangeTracker，所以 updated_at 要自己写。
        var now = DateTimeOffset.UtcNow;

        await db.TodoItems
            .Where(x => x.UserId == userId && x.ListId == listId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.ListId, (int?)null)
                    .SetProperty(x => x.UpdatedAt, now),
                ct);

        // Remove 会被 TodoSystemDbContext 的审计逻辑转成 UPDATE deleted_at
        db.TodoLists.Remove(list);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>查询条件：主键 + 归属，两者必须同时成立。</summary>
    private static Expression<Func<TodoListEntity, bool>> Owned(int userId, int listId)
        => x => x.Id == listId && x.UserId == userId;

    /// <summary>
    /// 找不到、或者找到了但不属于该用户，都抛 404。刻意不区分这两种情况：
    /// 返回 403 等于告诉调用方「这个 id 是存在的，只是不是你的」，把别人清单的存在性泄漏了出去。
    /// </summary>
    private async Task<TodoListEntity> FindAsync(int userId, int listId, CancellationToken ct)
        => await db.TodoLists.FirstOrDefaultAsync(Owned(userId, listId), ct)
           ?? throw new CustomException(404, "清单不存在");

    private IQueryable<TodoListDto> Project(IQueryable<TodoListEntity> query)
        => query.Select(x => new TodoListDto(
            x.Id,
            x.Name,
            x.Description,
            x.Color,
            x.SortOrder,
            x.IsArchived,
            // 计数用显式相关子查询，不用 x.Items.Count() —— 复合外键的集合导航 EF 翻译不了，
            // 所以 TodoListEntity 上根本没有 Items 属性。软删除过滤器会自动作用到 db.TodoItems。
            db.TodoItems.Count(item => item.UserId == x.UserId && item.ListId == x.Id),
            db.TodoItems.Count(item =>
                item.UserId == x.UserId && item.ListId == x.Id && item.Status == TodoStatuses.Done),
            x.CreatedAt,
            x.UpdatedAt));
}
