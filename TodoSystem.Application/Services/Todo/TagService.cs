using Microsoft.EntityFrameworkCore;
using TodoSystem.Application.Common;
using TodoSystem.Application.Services.Todo.Dtos;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Application.Services.Todo;

public class TagService(TodoSystemDbContext db) : ITagService
{
    private const int MaxNameLength = 50;
    private const int MaxColorLength = 20;

    public async Task<IReadOnlyList<TagDto>> ListAsync(int userId, CancellationToken ct = default)
    {
        return await db.Tags
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new TagDto(x.Id, x.Name, x.Color))
            .ToListAsync(ct);
    }

    public async Task<TagDto> CreateAsync(
        int userId,
        CreateTagRequest request,
        CancellationToken ct = default)
    {
        var name = TodoValidation.RequireText(request.Name, "标签名称", MaxNameLength);

        var tag = new TagEntity
        {
            UserId = userId,
            Name = name,
            Color = TodoValidation.OptionalText(request.Color, "颜色", MaxColorLength),
        };

        db.Tags.Add(tag);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            db.Entry(tag).State = EntityState.Detached;
            throw new CustomException(409, $"已存在名为「{name}」的标签");
        }

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }

    public async Task<TagDto> UpdateAsync(
        int userId,
        int tagId,
        UpdateTagRequest request,
        CancellationToken ct = default)
    {
        var tag = await FindAsync(userId, tagId, ct);
        var name = TodoValidation.RequireText(request.Name, "标签名称", MaxNameLength);

        tag.Name = name;
        tag.Color = TodoValidation.OptionalText(request.Color, "颜色", MaxColorLength);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new CustomException(409, $"已存在名为「{name}」的标签");
        }

        return new TagDto(tag.Id, tag.Name, tag.Color);
    }

    public async Task DeleteAsync(int userId, int tagId, CancellationToken ct = default)
    {
        var tag = await FindAsync(userId, tagId, ct);

        // 关联行必须硬删除。tags 是软删除的，而软删除不会触发数据库的 ON DELETE CASCADE
        // （级联只在真正的 DELETE 时发生），关联行会全部留在 todo_item_tags 里指向一个
        // 已删除的标签 —— 不显示，但会一直堆积，每次查待办标签都要白跑一遍。
        await db.TodoItemTags
            .Where(x => x.TagId == tagId)
            .ExecuteDeleteAsync(ct);

        db.Tags.Remove(tag);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>找不到或不属于该用户都返回 404，理由同 <see cref="TodoListService"/>。</summary>
    private async Task<TagEntity> FindAsync(int userId, int tagId, CancellationToken ct)
        => await db.Tags.FirstOrDefaultAsync(x => x.Id == tagId && x.UserId == userId, ct)
           ?? throw new CustomException(404, "标签不存在");
}
