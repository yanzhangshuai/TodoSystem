using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("v{Version:apiVersion}/[controller]")]
public class AccountController(IAuthService authService) : ControllerBase
{
    /// <summary>登录：写 Cookie（同站浏览器）。</summary>
    [HttpPost("login"), MapToApiVersion("1")]
    [AllowAnonymous]
    public async Task<ApiResult> LoginCookieAsync([FromBody] LoginInput input)
    {
        var user = await authService.AuthenticateAsync(new LoginRequest(input.UserName, input.Password));

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaims(
        [
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
        ]);
        
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
           new AuthenticationProperties
        {
            IsPersistent = true
        });

        return ApiResult.Ok();
    }
    
    /// <summary>登出：清理 Cookie。</summary>
    [HttpPost("logout"), MapToApiVersion("1")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ApiResult> LogoutCookieAsync([FromBody] LogoutInput? input)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return ApiResult.Ok();
    }
    
    
    /// <summary>登录：返回 JWT（API 客户端）。</summary>
    [HttpPost("login"), MapToApiVersion("2")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> LoginTokenAsync([FromBody] LoginInput input)
    {
        var result = await authService.LoginTokenAsync(new LoginRequest(input.UserName, input.Password));

        return ApiResult<LoginResult>.Ok(result);
    }
    
    /// <summary>登出：清理 jwt。</summary>
    [HttpPost("logout"), MapToApiVersion("2")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ApiResult> LogoutTokenAsync([FromBody] LogoutInput? input)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        await authService.LogoutAsync(userId, input?.RefreshToken);
        return ApiResult.Ok();
    }


    /// <summary>用 Refresh Token 换新令牌（轮换）。</summary>
    [HttpPost("refresh"), MapToApiVersion("2")]
    [AllowAnonymous]
    public async Task<ApiResult<LoginResult>> RefreshTokenAsync([FromBody] RefreshRequest input)
    {
        var result = await authService.RefreshAsync(input);
        return ApiResult<LoginResult>.Ok(result);
    }
    
}

public class LoginInput
{
    public string UserName { get; set; } = default!;

    public string Password { get; set; } = default!;
}

public class LogoutInput
{
    /// <summary>可选。只吊销指定 Refresh Token；为空则吊销当前用户全部。</summary>
    public string? RefreshToken { get; set; } = default!;
}
