using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Web.Controllers.Requests;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 账号操作：当前登录用户对自己资料的读写。
///
/// 划分判据只有一条：会改变当前会话凭证的操作（注册、登录、刷新、登出）
/// 归 <see cref="AuthController"/>，只是动账号本身的归这里。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/[controller]")]
[Authorize]
public class AccountController(IAuthService authService) : ControllerBase
{
    /// <summary>当前登录用户资料。JWT 和 Cookie 两种方案都能调。</summary>
    [HttpGet("me")]
    public async Task<ApiResult<UserProfile>> MeAsync(CancellationToken ct)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        if (userId <= 0)
        {
            throw new CustomException(401, "未登录");
        }

        var profile = await authService.GetProfileAsync(userId, ct);

        return ApiResult<UserProfile>.Ok(profile);
    }

    /// <summary>
    /// 把 Google 账号绑定到当前用户。
    ///
    /// 要求先登录，等于已经用密码证明过账号归属，所以这里允许建立绑定 ——
    /// 这也是「未验证邮箱不被第三方登录自动合并」之后唯一的补绑通道。
    /// </summary>
    [HttpPost("google")]
    public async Task<ApiResult<UserProfile>> LinkGoogleAsync(
        [FromBody] GoogleIdTokenInput input,
        CancellationToken ct)
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        if (userId <= 0)
        {
            throw new CustomException(401, "未登录");
        }

        var profile = await authService.LinkGoogleAsync(
            userId,
            new GoogleLoginRequest(input.IdToken),
            ct);

        return ApiResult<UserProfile>.Ok(profile);
    }
}
