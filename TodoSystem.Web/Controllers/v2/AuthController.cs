using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Web.Controllers.Requests;
using TodoSystem.Web.Controllers.v1;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v2;

/// <summary>
/// 凭证方案 v2：JWT + Refresh Token，给 SPA / API 客户端用。
///
/// 与 v1（<see cref="CookieAuthController"/>）的区别不只是响应体带不带令牌：
/// 这一套的会话是无状态的 —— Access Token 自带过期时间，续期靠 refresh 动作
/// 轮换 Refresh Token，登出靠吊销服务端的 Refresh Token 记录。
/// </summary>
[ApiController]
[ApiVersion("2")]
[Route("v{Version:apiVersion}/auth")]
public class TokenAuthController(IAuthService authService) : ControllerBase
{
    /// <summary>
    /// 本地账号注册，成功后直接签发令牌。
    /// 注册不验证邮箱归属，新账号邮箱处于「未验证」状态。
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> RegisterAsync(
        [FromBody] RegisterInput input,
        CancellationToken ct)
    {
        var result = await authService.RegisterAsync(
            new RegisterRequest(input.UserName, input.Email, input.Password),
            ct);

        return ApiResult<LoginResult>.Ok(result);
    }

    /// <summary>JWT 登录：返回 Access Token + Refresh Token。</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> LoginAsync([FromBody] LoginInput input)
    {
        var result = await authService.LoginTokenAsync(
            new LoginRequest(input.UserName, input.Password));

        return ApiResult<LoginResult>.Ok(result);
    }

    /// <summary>
    /// 用 Refresh Token 换取新的 Access Token + Refresh Token（轮换）。
    /// 传入已被轮换且超出宽限窗口的令牌，判定为重放并吊销该用户全部令牌。
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> RefreshAsync([FromBody] RefreshRequest input)
    {
        var result = await authService.RefreshAsync(input);

        return ApiResult<LoginResult>.Ok(result);
    }

    /// <summary>
    /// JWT 登出：吊销 Refresh Token。
    /// 不传 <c>refreshToken</c> 则吊销当前用户全部令牌（等于所有设备下线）。
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ApiResult> LogoutAsync([FromBody] LogoutInput? input)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        if (userId <= 0)
        {
            throw new CustomException(401, "未登录");
        }

        await authService.LogoutAsync(userId, input?.RefreshToken);

        return ApiResult.Ok();
    }

    /// <summary>
    /// Google 登录 / 首次登录即注册，返回 Access Token + Refresh Token。
    ///
    /// 若同邮箱的本地账号邮箱未验证，返回 409 —— 需先用密码登录，
    /// 再调 <c>POST /api/v1/account/google</c> 主动绑定。
    /// </summary>
    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> GoogleLoginAsync(
        [FromBody] GoogleIdTokenInput input,
        CancellationToken ct)
    {
        var result = await authService.LoginWithGoogleAsync(
            new GoogleLoginRequest(input.IdToken),
            ct);

        return ApiResult<LoginResult>.Ok(result);
    }
}
