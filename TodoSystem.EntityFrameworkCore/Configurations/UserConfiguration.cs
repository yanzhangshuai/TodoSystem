using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;

namespace TodoSystem.EntityFrameworkCore.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    /// <summary>
    /// 唯一索引必须带这个过滤条件：全局查询过滤器让重复校验查不到软删除的行，
    /// 而普通唯一索引照样拦插入，注销过的邮箱就再也注册不上（且报 500 而非 409）。
    /// </summary>
    private const string NotDeleted = "deleted_at IS NULL";

    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        // 角色取值在数据库层兜底，防止绕过 EF 写入脏数据
        builder.ToTable("users", t => t.HasCheckConstraint(
            "ck_users_role",
            $"role IN ('{UserRoles.User}', '{UserRoles.Admin}')"));

        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Password).HasMaxLength(255);
        builder.Property(x => x.Role).IsRequired().HasMaxLength(20).HasDefaultValue(UserRoles.User);
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);

        // 邮箱是第三方登录合并账号的依据，必须唯一，否则合并逻辑会产生歧义
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter(NotDeleted)
            .HasDatabaseName("ix_users_email");

        // 用户名同样是登录入口（AuthenticateAsync 允许用用户名登录），
        // 不唯一会让「按用户名登录」取到不确定的一行，所以这里也必须是唯一索引
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter(NotDeleted)
            .HasDatabaseName("ix_users_name");
    }
}
