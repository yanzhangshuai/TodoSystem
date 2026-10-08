namespace TodoSystem.Domain.Constants;

/// <summary>
/// 第三方登录提供商标识，落库到 <c>external_logins.provider</c>。
/// 统一小写，避免 "Google" / "google" 被当成两家。
/// </summary>
public static class AuthProviders
{
    public const string Google = "google";
}
