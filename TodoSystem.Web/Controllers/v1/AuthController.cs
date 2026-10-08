using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Web.Controllers.Requests;
using TodoSystem.Web.Controllers.v2;
using TodoSystem.Web.Utils.ApiResults;
using TodoSystem.Web.Utils.Auth;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 凭证方案 v1：Cookie，给同站浏览器用。
///
/// 四个动作都不产生 JWT / Refresh Token，会话状态完全由认证 Cookie 承载，
/// 续期交给 Cookie 的滑动过期，因此没有 refresh 动作。这是与
/// <see cref="TokenAuthController"/>（v2，JWT）的核心区别。
///
/// 路由写死 <c>auth</c> 而不用 <c>[controller]</c>：后者会展开成 <c>cookieauth</c>，
/// 路径就变成 <c>/api/v1/cookieauth/*</c>。两个版本的控制器共用同一个路由模板，
/// 靠 <c>[ApiVersion]</c> 区分。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/auth")]
public class CookieAuthController(IAuthService authService) : ControllerBase
{
    /// <summary>本地账号注册，成功后建立 Cookie 会话，响应体不带令牌。</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResult> RegisterAsync(
        [FromBody] RegisterInput input,
        CancellationToken ct)
    {
        var user = await authService.CreateLocalUserAsync(
            new RegisterRequest(input.UserName, input.Email, input.Password),
            ct);

        await HttpContext.SignInWithCookieAsync(user);

        return ApiResult.Ok();
    }

    /// <summary>Cookie 登录：写认证 Cookie，不产生任何令牌。</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResult> LoginAsync([FromBody] LoginInput input)
    {
        var user = await authService.AuthenticateAsync(
            new LoginRequest(input.UserName, input.Password));

        await HttpContext.SignInWithCookieAsync(user);

        return ApiResult.Ok();
    }

    /// <summary>
    /// 清除认证 Cookie。
    /// Cookie 已过期时这里会 401（取不到身份），不影响客户端清理本地状态。
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ApiResult> LogoutAsync()
    {
        await HttpContext.SignOutCookieAsync();

        return ApiResult.Ok();
    }

    /// <summary>
    /// Google 登录 / 首次登录即注册，成功后建立 Cookie 会话。
    /// 只解析用户、不签发令牌。
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ApiResult> GoogleLoginAsync(
        [FromBody] GoogleIdTokenInput input,
        CancellationToken ct)
    {
        var user = await authService.ResolveGoogleUserAsync(
            new GoogleLoginRequest(input.IdToken),
            ct);

        await HttpContext.SignInWithCookieAsync(user);

        return ApiResult.Ok();
    }
}
