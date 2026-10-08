namespace TodoSystem.Application.Services.Todo.Dtos;

/// <summary>待办读模型（扁平）。子任务不内嵌，用 <see cref="ParentId"/> 自行组织。</summary>
public record TodoItemDto(
    int Id,
    int? ListId,
    string? ListName,
    int? ParentId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTimeOffset? DueAt,
    DateTimeOffset? CompletedAt,
    int SortOrder,
    bool IsOverdue,
    int ChildCount,
    int CompletedChildCount,
    IReadOnlyList<TagDto> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>单条待办的详情：在扁平模型之外带上它的子任务（层级最多两层，所以只有一层）。</summary>
public record TodoItemDetailDto(
    TodoItemDto Item,
    IReadOnlyList<TodoItemDto> Children);

/// <summary>列表筛选条件。字段全为空表示「该用户的全部待办」。</summary>
public class TodoItemQuery
{
    /// <summary>只看某个清单。<c>0</c> 无特殊含义，不要用它表示「收件箱」。</summary>
    public int? ListId { get; set; }

    /// <summary>
    /// 只看未归入任何清单的待办（list_id IS NULL），也就是界面上的「收件箱」。
    /// 不能靠 ListId 传 null 表达 —— 那个 null 的含义是「不按清单过滤」。
    /// </summary>
    public bool? Unassigned { get; set; }

    /// <summary>只看某个父任务下的子任务。</summary>
    public int? ParentId { get; set; }

    /// <summary>只看带某个标签的待办。</summary>
    public int? TagId { get; set; }

    /// <summary>
    /// 状态白名单。用集合而不是单值，是为了能表达「未完成的」这种最常见的诉求：
    /// <c>?statuses=TODO&amp;statuses=IN_PROGRESS</c>。单值参数做不到，只能再加一个
    /// <c>excludeCompleted</c> 开关，两个参数语义还互相重叠。
    /// </summary>
    public string[]? Statuses { get; set; }

    /// <summary>优先级精确匹配。</summary>
    public string? Priority { get; set; }

    /// <summary>截止时间早于等于该时刻（用于「今天到期」）。</summary>
    public DateTimeOffset? DueBefore { get; set; }

    /// <summary>标题关键字，不区分大小写，<c>%</c> 与 <c>_</c> 按字面处理。</summary>
    public string? Keyword { get; set; }
}

public record CreateTodoItemRequest(
    string Title,
    string? Description,
    int? ListId,
    int? ParentId,
    string? Status,
    string? Priority,
    DateTimeOffset? DueAt,
    int? SortOrder);

/// <summary>
/// 整体替换语义（PUT）。刻意不含 ParentId：允许改父任务就要处理「把父任务改成自己的
/// 子任务」这类环，还要连带搬动整棵子树，收益远小于复杂度。要换父任务就删掉重建。
/// </summary>
public record UpdateTodoItemRequest(
    string Title,
    string? Description,
    int? ListId,
    string Status,
    string Priority,
    DateTimeOffset? DueAt,
    int? SortOrder);

/// <summary>单独改状态。这是最高频的动作，不值得为了它回传整个对象。</summary>
public record ChangeTodoStatusRequest(string Status);
