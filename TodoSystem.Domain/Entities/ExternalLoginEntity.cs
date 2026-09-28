using System.ComponentModel.DataAnnotations.Schema;
using TodoSystem.Domain.Base;

namespace TodoSystem.Domain.Entities;

/// <summary>
/// 第三方登录绑定关系。一个本地用户可以绑定多个提供商（Google / GitHub / 微信…），
/// 一个提供商的账号也只能绑定到一个本地用户。
/// </summary>
[Table("external_logins")]
public class ExternalLoginEntity : AppBaseEntity
{
    /// <summary>提供商名称，统一小写，如 google。</summary>
    public string Provider { get; set; } = default!;

    /// <summary>
    /// 提供商侧的用户唯一标识。
    /// Google 对应 id_token 里的 sub（不可变，改邮箱也不会变），不要用 email 当主键。
    /// </summary>
    public string ProviderKey { get; set; } = default!;

    public int UserId { get; set; }

    /// <summary>绑定时的邮箱快照，便于排查；不作为身份依据。</summary>
    public string? Email { get; set; }

    /// <summary>提供商返回的昵称快照。</summary>
    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>提供商返回的原始声明 JSON，保留以便后续扩展（如取 locale）。</summary>
    public string? RawClaims { get; set; }

    public UserEntity? User { get; set; }
}
