using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationTokenEntity>
{
    public void Configure(EntityTypeBuilder<EmailVerificationTokenEntity> builder)
    {
        // Base64(SHA-256) 固定 44 字符，给 64 足够余量
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);

        // 验证时按哈希查一次。不需要部分唯一索引：哈希随机，不会因软删除而重复。
        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ix_email_verification_tokens_token_hash");

        // 重发时按 user_id 作废该用户尚未失效的令牌
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_email_verification_tokens_user_id");

        // 实体没有导航属性时，只配索引不会生成外键，完整性只能靠应用层
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("fk_email_verification_tokens_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
