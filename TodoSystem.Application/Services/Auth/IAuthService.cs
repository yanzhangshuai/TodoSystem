using TodoSystem.Application.Services.Auth.Dtos;

namespace TodoSystem.Application.Services.Auth;

public interface IAuthService
{
    /// <summary>验证用户名、密码与账号状态。不产生 JWT / Refresh Token。</summary>
    Task<UserAuthInfo> AuthenticateAsync(
        LoginRequest request,
        CancellationToken ct = default);

    /// <summary>JWT 登录：验证用户后签发 Access Token + Refresh Token。</summary>
    Task<LoginResult> LoginTokenAsync(
        LoginRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// 本地账号注册，只创建用户、不签发令牌（给 Cookie 方案用）。
    /// 注册时邮箱未经证实，所以会发一封验证邮件；发信失败不影响注册结果。
    /// </summary>
    Task<UserAuthInfo> CreateLocalUserAsync(
        RegisterRequest request,
        CancellationToken ct = default);

    /// <summary>本地账号注册 + 签发令牌（给 JWT 方案用）。</summary>
    Task<LoginResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Google 登录 / 首次登录即注册，只解析用户、不签发令牌（给 Cookie 方案用）。
    ///
    /// 匹配顺序：先按 (provider, sub) 找绑定，再按已验证邮箱合并到已有本地账号，
    /// 都没有才新建。同邮箱的本地账号若邮箱未验证，抛 409，要求先密码登录再主动绑定。
    /// </summary>
    Task<UserAuthInfo> ResolveGoogleUserAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default);

    /// <summary>Google 登录 + 签发令牌（给 JWT 方案用）。</summary>
    Task<LoginResult> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// 把 Google 账号绑定到当前已登录用户。这是「本地账号后来想用 Google 登录」的
    /// 唯一安全通道：调用者必须已用密码证明身份。
    /// </summary>
    Task<UserProfile> LinkGoogleAsync(
        int userId,
        GoogleLoginRequest request,
        CancellationToken ct = default);

    Task<UserProfile> GetProfileAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// 用 Refresh Token 换新 JWT（轮换）。
    /// 传入已吊销的令牌会被判定为重放，并吊销该用户全部令牌。
    /// </summary>
    Task<LoginResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken ct = default);

    Task LogoutAsync(
        int userId,
        string? refreshToken,
        CancellationToken ct = default);
}
