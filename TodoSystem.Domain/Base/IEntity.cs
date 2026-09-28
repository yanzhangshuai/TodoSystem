using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoSystem.Domain.Base;

public interface IEntity<TKey>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public TKey Id { get; set; }
}

/// <summary>
/// 主键位 int类型的基类
/// </summary>
public interface IEntityInt : IEntity<int>
{
    
}

/// <summary>
/// 主键位 Guid 类型的基类
/// </summary>
public interface IEntityGuid : IEntity<Guid>
{
    
}