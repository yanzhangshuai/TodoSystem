using TodoSystem.Application.Services.Todo.Dtos;

namespace TodoSystem.Application.Services.Todo;

/// <summary>标签的业务操作。标签是用户私有的，userId 的作用同 <see cref="ITodoListService"/>。</summary>
public interface ITagService
{
    Task<IReadOnlyList<TagDto>> ListAsync(
        int userId,
        CancellationToken ct = default);

    Task<TagDto> CreateAsync(
        int userId,
        CreateTagRequest request,
        CancellationToken ct = default);

    Task<TagDto> UpdateAsync(
        int userId,
        int tagId,
        UpdateTagRequest request,
        CancellationToken ct = default);

    /// <summary>软删除标签，并硬删除它与待办的关联行（关联表没有软删除语义）。</summary>
    Task DeleteAsync(
        int userId,
        int tagId,
        CancellationToken ct = default);
}
