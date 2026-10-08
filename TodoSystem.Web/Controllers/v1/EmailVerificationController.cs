using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Web.Controllers.Requests;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 邮箱验证。两个动作都匿名可调 —— 用户是从邮件里点进来的，那时未必有会话。
/// 与凭证方案无关，所以只有 v1。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/email-verification")]
[AllowAnonymous]
public class EmailVerificationController(IEmailVerificationService emailVerification) : ControllerBase
{
    /// <summary>消费邮件里的令牌，成功后账号邮箱变为「已验证」。</summary>
    [HttpPost("verify")]
    public async Task<ApiResult<VerifyEmailResult>> VerifyAsync(
        [FromBody] VerifyEmailInput input,
        CancellationToken ct)
        => ApiResult<VerifyEmailResult>.Ok(
            await emailVerification.VerifyAsync(input.Token, ct));

    /// <summary>
    /// 重发验证邮件。邮箱不存在、已验证、或还在冷却期内都返回成功 ——
    /// 匿名可调的端点上，响应里的任何差异都会变成账号探测手段。
    /// </summary>
    [HttpPost("resend")]
    public async Task<ApiResult> ResendAsync(
        [FromBody] ResendVerificationInput input,
        CancellationToken ct)
    {
        await emailVerification.ResendAsync(input.Email, ct);

        return ApiResult.Ok();
    }
}
