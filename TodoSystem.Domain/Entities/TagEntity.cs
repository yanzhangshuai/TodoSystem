using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 标签。和清单一样是用户私有的，不同用户的同名标签互不相干。
/// 与待办的多对多关系落在 <see cref="TodoItemTagEntity"/> 上。
/// </summary>
[Table("tags")]
public class TagEntity : AppBaseEntity
{
    public int UserId { get; set; }

    /// <summary>同一用户、未删除范围内唯一（部分唯一索引 ix_tags_user_id_name）。</summary>
    public string Name { get; set; } = default!;

    /// <summary>前端展示色，后端不解释其语义。</summary>
    public string? Color { get; set; }

    public UserEntity? User { get; set; }

    public ICollection<TodoItemTagEntity> ItemTags { get; set; } = new List<TodoItemTagEntity>();
}
