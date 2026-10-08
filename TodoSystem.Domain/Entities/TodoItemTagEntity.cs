using System.ComponentModel.DataAnnotations.Schema;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 待办 ↔ 标签的多对多关联表。
///
/// 刻意不继承 AppBaseEntity：关联表没有独立生命周期 ——「挂上标签」是插入一行，
/// 「摘掉标签」是删除一行，不存在「修改关联」，所以 UpdatedAt / DeletedAt 永远没有意义。
/// 复合主键 (todo_item_id, tag_id) 本身就够用，还顺带保证同一个标签不会被重复挂上。
/// </summary>
[Table("todo_item_tags")]
public class TodoItemTagEntity
{
    public int TodoItemId { get; set; }

    public int TagId { get; set; }

    public TodoItemEntity? TodoItem { get; set; }

    public TagEntity? Tag { get; set; }
}
