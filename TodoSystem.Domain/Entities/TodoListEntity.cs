using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 待办清单：用户自己的分组。
/// 归属由 UserId 唯一确定，清单不共享 —— 服务层的每个查询都必须带上这个条件。
/// </summary>
[Table("todo_lists")]
public class TodoListEntity : AppBaseEntity
{
    public int UserId { get; set; }

    /// <summary>同一用户、未删除范围内唯一（部分唯一索引 ix_todo_lists_user_id_name）。</summary>
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

    /// <summary>前端展示色，后端不解释其语义。</summary>
    public string? Color { get; set; }

    /// <summary>手动排序序号，升序展示。</summary>
    public int SortOrder { get; set; }

    /// <summary>归档后默认不出现在清单列表里，数据保留、可随时恢复。</summary>
    public bool IsArchived { get; set; }

    public UserEntity? User { get; set; }

    // 刻意没有 ICollection<TodoItemEntity> Items。
    // 待办 → 清单的外键是复合的 (user_id, list_id) → (user_id, id)，而 EF Core
    // 翻译不了这个复合外键的集合导航（生成 object.Equals(object[], object[]) 数组比较，
    // 运行时抛 could not be translated）。同一外键的引用导航 List 不受影响。
    // 需要清单下的待办就直接查 TodoItems，itemCount / completedCount 就是这么算的。
}
