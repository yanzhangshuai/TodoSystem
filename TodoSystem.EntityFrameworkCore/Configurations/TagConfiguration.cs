using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
    /// <summary>见 <see cref="UserConfiguration"/> 中同名常量的说明。</summary>
    private const string NotDeleted = "deleted_at IS NULL";

    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Color).HasMaxLength(20);

        // 同一用户下标签不重名（未删除范围内）。
        // 标签是用户私有的，所以唯一性必须带上 user_id —— 全局唯一会让
        // 「我建了个叫 work 的标签」把别人的 work 占掉。
        builder.HasIndex(x => new { x.UserId, x.Name })
            .IsUnique()
            .HasFilter(NotDeleted)
            .HasDatabaseName("ix_tags_user_id_name");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_tags_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
