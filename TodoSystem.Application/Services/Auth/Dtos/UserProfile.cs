namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>
/// 当前登录用户的资料。
/// </summary>
/// <param name="HasLocalPassword">是否设置了本地密码；false 表示只能走第三方登录。</param>
/// <param name="EmailVerified">邮箱是否已验证。未验证时，第三方登录不会自动合并到本账号。</param>
public record UserProfile(
    int UserId,
    string UserName,
    string Email,
    string Role,
    string? AvatarUrl,
    bool HasLocalPassword,
    bool HasGoogleBound,
    bool EmailVerified);
