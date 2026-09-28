namespace TodoSystem.Infrastructure.Token;

/// <summary>
/// JWT 签发/有效期配置。签发与验证共用，避免两侧参数不一致。
/// </summary>
public sealed class TokenOptions
{
    public const string SectionName = "Jwt";

    public string SecretKey { get; set; } = default!;

    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    /// <summary>Access Token 有效期（分钟），宜短</summary>
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Refresh Token 有效期（天）</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
