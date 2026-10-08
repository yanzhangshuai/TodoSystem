using TodoSystem.Application.Services.Todo.Dtos;

namespace TodoSystem.Application.Services.Todo;

/// <summary>
/// 待办的业务操作。跨表一致性中数据库只兜住了一部分，剩下的在这里：
/// 清单归属由复合外键保证（这里只负责给友好报错）；子任务层级（最多两层）和同用户
/// 数据库表达不了，只能在这里校验；completed_at 与 status 的同步也在这里。
/// </summary>
public interface ITodoItemService
{
    /// <summary>按条件列出该用户的待办。</summary>
    Task<IReadOnlyList<TodoItemDto>> ListAsync(
        int userId,
        TodoItemQuery query,
        CancellationToken ct = default);

    /// <summary>取单条待办详情，带上它的子任务。</summary>
    Task<TodoItemDetailDto> GetAsync(
        int userId,
        int itemId,
        CancellationToken ct = default);

    Task<TodoItemDto> CreateAsync(
        int userId,
        CreateTodoItemRequest request,
        CancellationToken ct = default);

    Task<TodoItemDto> UpdateAsync(
        int userId,
        int itemId,
        UpdateTodoItemRequest request,
        CancellationToken ct = default);

    /// <summary>改状态，并同步完成时间。</summary>
    Task<TodoItemDto> ChangeStatusAsync(
        int userId,
        int itemId,
        string status,
        CancellationToken ct = default);

    /// <summary>软删除待办，连同它的子任务。</summary>
    Task DeleteAsync(
        int userId,
        int itemId,
        CancellationToken ct = default);

    /// <summary>挂标签（幂等：已挂上的忽略）。</summary>
    Task<TodoItemDto> AttachTagsAsync(
        int userId,
        int itemId,
        IReadOnlyList<int> tagIds,
        CancellationToken ct = default);

    /// <summary>摘掉一个标签（幂等：没挂过也返回成功）。</summary>
    Task<TodoItemDto> DetachTagAsync(
        int userId,
        int itemId,
        int tagId,
        CancellationToken ct = default);
}
