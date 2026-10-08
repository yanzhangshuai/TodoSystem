namespace TodoSystem.Application.Services.Todo.Dtos;

/// <summary>
/// 清单读模型。ItemCount / CompletedCount 是查询时聚合出来的，不是 todo_lists 的字段 ——
/// 存了就要在每次增删改待办时同步维护，是典型的「为了省一次 COUNT 换来一类不一致」。
/// </summary>
public record TodoListDto(
    int Id,
    string Name,
    string? Description,
    string? Color,
    int SortOrder,
    bool IsArchived,
    int ItemCount,
    int CompletedCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateTodoListRequest(
    string Name,
    string? Description,
    string? Color,
    int? SortOrder);

/// <summary>
/// 整体替换语义（PUT）：请求体就是清单的目标状态。未提供的可空字段（Description / Color）
/// 会被置空，而不是「保持原值」—— 这样「清空说明」和「不修改说明」不需要两套语义。
/// </summary>
public record UpdateTodoListRequest(
    string Name,
    string? Description,
    string? Color,
    int? SortOrder,
    bool IsArchived);
