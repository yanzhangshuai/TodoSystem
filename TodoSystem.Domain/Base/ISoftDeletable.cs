namespace TodoSystem.Domain.Base;

public interface ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; set; }
}