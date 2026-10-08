namespace TodoSystem.Application.Services.Auth;

/// <summary>
/// 认证域共用的入参规范化。集中一处，避免注册、登录、重发各写一遍 ——
/// 分叉的后果是同一个邮箱在两张表里长得不一样，然后「查不到这个账号」。
/// </summary>
internal static class AuthValidation
{
    /// <summary>邮箱统一去空格 + 转小写后再比较和落库。</summary>
    public static string NormalizeEmail(string? email)
        => (email ?? string.Empty).Trim().ToLowerInvariant();
}
