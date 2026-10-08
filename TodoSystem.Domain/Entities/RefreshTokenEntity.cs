using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;
using TodoSystem.Domain.Constants;

namespace TodoSystem.Domain.Entities;

[Table("refresh_tokens")]
public class RefreshTokenEntity : AppBaseEntity
{
    /// <summary>刷新令牌哈希（Base64(SHA-256)，固定 44 字符），避免落库明文</summary>
    public string TokenHash { get; set; } = default!;

    public int UserId { get; set; }

    /// <summary>
    /// 过期时间（UTC）。
    /// 用 DateTimeOffset 而不是 DateTime：Npgsql 会把 DateTime 按 Kind 分别映射到
    /// timestamptz / timestamp，同一个属性会有两种列语义，写一次 DateTime.Now 就静默错时区。
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>吊销时间（UTC）；非空表示已失效（登出、轮换或检测到重放）</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>吊销原因，取值见 <see cref="RefreshTokenRevocationReasons"/>，用于审计与安全排查</summary>
    public string? RevokedReason { get; set; }

    /// <summary>轮换后的新令牌哈希，便于审计追溯</summary>
    public string? ReplacedByTokenHash { get; set; }

    public UserEntity? User { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
