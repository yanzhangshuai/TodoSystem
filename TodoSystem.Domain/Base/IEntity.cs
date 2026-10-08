using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoSystem.Domain.Base;

public interface IEntity<TKey>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public TKey Id { get; set; }
}

/// <summary>主键为 int 的实体基接口。</summary>
public interface IEntityInt : IEntity<int>
{
}

/// <summary>主键为 Guid 的实体基接口。</summary>
public interface IEntityGuid : IEntity<Guid>
{
}