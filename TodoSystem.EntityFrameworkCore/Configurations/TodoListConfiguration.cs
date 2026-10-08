using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class TodoListConfiguration : IEntityTypeConfiguration<TodoListEntity>
{
    /// <summary>见 <see cref="UserConfiguration"/> 中同名常量的说明。</summary>
    private const string NotDeleted = "deleted_at IS NULL";

    public void Configure(EntityTypeBuilder<TodoListEntity> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Color).HasMaxLength(20);

        // 同一用户下不允许重名清单（未删除范围内）。
        // 带 filter 是必须的：否则删掉一个清单后，同名清单再也建不出来。
        builder.HasIndex(x => new { x.UserId, x.Name })
            .IsUnique()
            .HasFilter(NotDeleted)
            .HasDatabaseName("ix_todo_lists_user_id_name");

        // 清单列表固定按 (user_id, sort_order) 取，复合索引让它可以只走索引
        builder.HasIndex(x => new { x.UserId, x.SortOrder })
            .HasDatabaseName("ix_todo_lists_user_id_sort_order");

        // 被 todo_items 的复合外键 (user_id, list_id) 引用，需要一个唯一约束当 principal key。
        // 它和上面的 (user_id, name) 唯一索引不是一回事：那个管名字不重复，
        // 这个管 (用户, 清单) 能被稳定引用。
        builder.HasAlternateKey(x => new { x.UserId, x.Id })
            .HasName("ak_todo_lists_user_id_id");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_todo_lists_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
