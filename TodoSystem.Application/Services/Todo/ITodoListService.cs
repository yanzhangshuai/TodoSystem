using TodoSystem.Application.Services.Todo.Dtos;

namespace TodoSystem.Application.Services.Todo;

/// <summary>
/// 待办清单的业务操作。
///
/// 每个方法的第一个参数都是 userId，且不是可选参数 —— 这不是冗余，而是数据隔离的唯一入口：
/// 做成必填，编译器就会逼着你交出这个值，不会出现「忘了过滤用户」的查询。
/// </summary>
public interface ITodoListService
{
    /// <summary>列出该用户的清单，按 SortOrder 升序。</summary>
    Task<IReadOnlyList<TodoListDto>> ListAsync(
        int userId,
        bool includeArchived,
        CancellationToken ct = default);

    /// <summary>取单个清单。不存在或不属于该用户都返回 404。</summary>
    Task<TodoListDto> GetAsync(
        int userId,
        int listId,
        CancellationToken ct = default);

    Task<TodoListDto> CreateAsync(
        int userId,
        CreateTodoListRequest request,
        CancellationToken ct = default);

    Task<TodoListDto> UpdateAsync(
        int userId,
        int listId,
        UpdateTodoListRequest request,
        CancellationToken ct = default);

    /// <summary>软删除清单。清单内的待办不删除，而是退回收件箱（list_id 置空）。</summary>
    Task DeleteAsync(
        int userId,
        int listId,
        CancellationToken ct = default);
}
