using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        // Base64(SHA-256) 固定 44 字符，给 64 足够余量
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        builder.Property(x => x.RevokedReason).HasMaxLength(50);

        // 每次刷新都按哈希查一次，加索引。
        // 这里不需要部分唯一索引：哈希是随机的，不会因为软删除而重复。
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token_hash");

        // 登出 / 检测到重放时按 user_id 批量吊销
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        // 实体没有导航属性时，只配索引不会生成外键，完整性只能靠应用层
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_refresh_tokens_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
