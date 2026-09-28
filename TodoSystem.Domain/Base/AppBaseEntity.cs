namespace TodoSystem.Domain.Base;

public abstract class AppBaseEntity: IEntityInt, ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}