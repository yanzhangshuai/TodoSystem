using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;
using TodoSystem.Domain.Constants;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 待办项。三个归属字段各自解决一个问题，不要合并：
/// UserId 是数据隔离的强制条件；ListId 是可选的清单分组（null 表示收件箱）；
/// ParentId 是可选的父待办，构成两层子任务结构。
/// </summary>
[Table("todo_items")]
public class TodoItemEntity : AppBaseEntity
{
    /// <summary>
    /// 与 ListId 一起构成指向 todo_lists 的复合外键，
    /// 由数据库保证「待办只能挂到自己名下的清单」—— 见 TodoItemConfiguration。
    /// </summary>
    public int UserId { get; set; }

    /// <summary>所属清单；null 表示未归类。</summary>
    public int? ListId { get; set; }

    /// <summary>父待办；null 表示顶层。层级最多两层，由服务层校验。</summary>
    public int? ParentId { get; set; }

    public string Title { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>取值见 <see cref="TodoStatuses"/>，数据库另有 ck_todo_items_status 兜底。</summary>
    public string Status { get; set; } = TodoStatuses.Todo;

    /// <summary>取值见 <see cref="TodoPriorities"/>，数据库另有 ck_todo_items_priority 兜底。</summary>
    public string Priority { get; set; } = TodoPriorities.Normal;

    /// <summary>截止时间（UTC）；null 表示不限期。</summary>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// 完成时间（UTC）；null 表示未完成。与 Status 的一致性由服务层维护。
    /// 刻意不从 Status 推导 ——「什么时候完成的」本身就是要展示和统计的信息。
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    public int SortOrder { get; set; }

    public UserEntity? User { get; set; }

    public TodoListEntity? List { get; set; }

    public TodoItemEntity? Parent { get; set; }

    public ICollection<TodoItemEntity> Children { get; set; } = new List<TodoItemEntity>();

    public ICollection<TodoItemTagEntity> ItemTags { get; set; } = new List<TodoItemTagEntity>();

    /// <summary>不落库，也翻译不成 SQL —— 查询里要用 CompletedAt 直接比较。</summary>
    [NotMapped]
    public bool IsCompleted => CompletedAt is not null;

    [NotMapped]
    public bool IsOverdue => CompletedAt is null && DueAt is { } due && due < DateTimeOffset.UtcNow;

    [NotMapped]
    public bool IsSubtask => ParentId is not null;
}
