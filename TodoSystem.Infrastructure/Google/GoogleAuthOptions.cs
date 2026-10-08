namespace TodoSystem.Infrastructure.Google;

/// <summary>
/// Google 登录配置。绑定 "Authentication:Google" 节点。
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>OAuth 2.0 客户端 ID。前端 GIS 和后端校验 aud 都要用，可公开。</summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// 客户端密钥。ID Token 直传流程其实用不到，
    /// 保留是为了将来接服务端授权码流程。不要提交到版本库。
    /// </summary>
    public string ClientSecret { get; set; } = default!;
}
