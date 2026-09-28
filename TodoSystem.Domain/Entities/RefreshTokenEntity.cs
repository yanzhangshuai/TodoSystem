using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

[Table("refresh_tokens")]
public class RefreshTokenEntity : AppBaseEntity
{
    /// <summary>刷新令牌哈希（SHA-256），避免落库明文</summary>
    public string TokenHash { get; set; } = default!;

    public int UserId { get; set; }

    public DateTime ExpiresAt { get; set; }


    /// <summary>吊销时间；非空表示已失效（登出或轮换）</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>轮换后的新令牌哈希，便于审计追溯</summary>
    public string? ReplacedByTokenHash { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

}
