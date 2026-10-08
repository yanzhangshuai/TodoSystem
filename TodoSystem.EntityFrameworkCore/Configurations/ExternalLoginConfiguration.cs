using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLoginEntity>
{
    /// <summary>见 <see cref="UserConfiguration"/> 中同名常量的说明。</summary>
    private const string NotDeleted = "deleted_at IS NULL";

    public void Configure(EntityTypeBuilder<ExternalLoginEntity> builder)
    {
        builder.Property(x => x.Provider).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(100);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);

        // 同一提供商的同一账号只能绑定一个本地用户（未删除范围内）。
        // 带 filter 是必须的：否则用户注销后，同一个 Google 账号再也绑不上新用户。
        builder.HasIndex(x => new { x.Provider, x.ProviderKey })
            .IsUnique()
            .HasFilter(NotDeleted)
            .HasDatabaseName("ix_external_logins_provider_provider_key");

        // 反查某用户绑定了哪些提供商
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_external_logins_user_id");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_external_logins_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
