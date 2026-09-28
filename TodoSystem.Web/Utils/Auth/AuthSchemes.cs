namespace TodoSystem.Web.Utils.Auth;

/// <summary>
/// 认证方案名。默认方案按请求头自动转发：
/// 带 Authorization: Bearer → JWT；否则 → Cookie。
/// </summary>
public static class AuthSchemes
{
    /// <summary>按请求特征转发的策略方案（默认）</summary>
    public const string BearerOrCookie = "BearerOrCookie";
}
