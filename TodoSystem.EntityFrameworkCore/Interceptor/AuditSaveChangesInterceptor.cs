using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoSystem.Domain.Base;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyChanges(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private static void ApplyChanges(DbContext? dbContext)
    {
        if (dbContext == null)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in dbContext.ChangeTracker.Entries<AppBaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.DeletedAt = null;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    // 把真正的 DELETE 转换成 UPDATE
                    entry.State = EntityState.Modified;

                    entry.Entity.DeletedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Detached:
                    break;
                case EntityState.Unchanged:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}