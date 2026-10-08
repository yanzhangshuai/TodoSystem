using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;
using TodoSystem.Domain.Constants;

namespace TodoSystem.Domain.Entities;

[Table("users")]
public class UserEntity : AppBaseEntity
{
    /// <summary>登录用户名。未删除范围内唯一（部分唯一索引 ix_users_name）。</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 密码哈希，禁止存明文。
    /// 纯第三方登录（如 Google）注册的账号没有本地密码，此字段为 null。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>邮箱。未删除范围内唯一，登录时可代替用户名使用。</summary>
    public string Email { get; set; } = default!;

    /// <summary>角色，取值见 <see cref="UserRoles"/>；数据库另有 ck_users_role 约束兜底。</summary>
    public string Role { get; set; } = UserRoles.User;

    /// <summary>账号是否可用。false 时拒绝一切登录方式。</summary>
    public bool IsActivated { get; set; }

    /// <summary>
    /// 邮箱完成归属验证的时间（UTC）；null 表示从未验证过。
    ///
    /// 只有非 null 的邮箱才允许被第三方登录自动合并到已有账号 —— 否则任何人抢先
    /// 用别人的邮箱注册，就能在对方用 Google 登录时把对方的账号劫持过来。
    /// </summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }

    /// <summary>头像地址。本地注册为空，第三方登录时由提供商回填。</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>最近一次成功登录时间（UTC）。</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>没有本地密码的账号只能走第三方登录。</summary>
    [NotMapped]
    public bool HasLocalPassword => !string.IsNullOrEmpty(Password);

    /// <summary>未验证的邮箱不能作为账号合并的依据。</summary>
    [NotMapped]
    public bool IsEmailVerified => EmailVerifiedAt is not null;
}
