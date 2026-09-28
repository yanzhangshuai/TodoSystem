using TodoSystem.Application.Services.Auth.Dtos;

namespace TodoSystem.Application.Services.Auth;

public interface IAuthService
{
    /// <summary>
    /// 验证用户名、密码以及账号状态。
    /// 不产生 JWT / Refresh Token。
    /// </summary>
    Task<UserAuthInfo> AuthenticateAsync(
        LoginRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// JWT 登录。
    /// 验证用户后生成 Access Token + Refresh Token。
    /// </summary>
    Task<LoginResult> LoginTokenAsync(
        LoginRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// 使用 Refresh Token 换取新的 JWT。
    /// </summary>
    Task<LoginResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// 注销，吊销 Refresh Token。
    /// </summary>
    Task LogoutAsync(
        int userId,
        string? refreshToken,
        CancellationToken ct = default);
}