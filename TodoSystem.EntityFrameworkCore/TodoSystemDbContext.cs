using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoSystem.Domain.Base;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore;

public class TodoSystemDbContext(DbContextOptions<TodoSystemDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    
    public DbSet<StudentEntity>  Students => Set<StudentEntity>();
    
    public DbSet<RefreshTokenEntity>  RefreshTokens => Set<RefreshTokenEntity>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        base.OnConfiguring(optionsBuilder);
        
        // 数据库字段小写
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TodoSystemDbContext).Assembly);

        // 自动给所有 ISoftDeletable 实体添加全局查询过滤器
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilters(
        ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model
                     .GetEntityTypes()
                     .Where(x => typeof(ISoftDeletable)
                         .IsAssignableFrom(x.ClrType)))
        {
            var parameter = Expression.Parameter(
                entityType.ClrType,
                "e");

            var property = Expression.Property(
                Expression.Convert(
                    parameter,
                    typeof(ISoftDeletable)),
                nameof(ISoftDeletable.DeletedAt));

            var nullValue = Expression.Constant(
                null,
                typeof(DateTimeOffset?));

            var body = Expression.Equal(
                property,
                nullValue);

            var lambda = Expression.Lambda(
                body,
                parameter);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(lambda);
        }
    }

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        ApplyAuditChanges();

        return base.SaveChanges(
            acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditChanges();

        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    private void ApplyAuditChanges()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            /*
             * CreatedAt / UpdatedAt
             */
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.UpdatedAt = now;
                        break;

                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        break;
                }
            }

            /*
             * Soft Delete
             */
            if (entry.Entity is not ISoftDeletable softDeletable ||
                entry.State != EntityState.Deleted) continue;
            // 把 DELETE 转换成 UPDATE
            entry.State = EntityState.Modified;

            softDeletable.DeletedAt = now;

            // 如果同时支持 IAuditable
            if (entry.Entity is IAuditable auditableEntity)
            {
                auditableEntity.UpdatedAt = now;
            }
        }
    }
    
}