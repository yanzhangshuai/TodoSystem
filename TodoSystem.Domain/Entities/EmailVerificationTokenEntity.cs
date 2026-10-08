using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 邮箱验证令牌。和 Refresh Token 一个思路：只存哈希，不存明文。
/// </summary>
[Table("email_verification_tokens")]
public class EmailVerificationTokenEntity : AppBaseEntity
{
    /// <summary>令牌哈希（Base64(SHA-256)，固定 44 字符），避免落库明文。</summary>
    public string TokenHash { get; set; } = default!;

    public int UserId { get; set; }

    /// <summary>发起验证时的邮箱快照，用于排查「这个链接验证的是哪个邮箱」。</summary>
    public string Email { get; set; } = default!;

    /// <summary>过期时间（UTC）。</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// 失效时间（UTC）。两种来源：被更新的令牌顶掉、或已被成功使用。
    /// 非空即不可再用。
    /// </summary>
    public DateTimeOffset? ConsumedAt { get; set; }

    public UserEntity? User { get; set; }
}
