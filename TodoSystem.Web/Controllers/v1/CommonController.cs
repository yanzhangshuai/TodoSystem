using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Infrastructure.Google;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 公开信息：不需要登录、也不涉及任何用户数据的接口。
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("v{Version:apiVersion}/[controller]")]
[AllowAnonymous]
public class CommonController(IOptions<GoogleAuthOptions> googleAuthOptions) : ControllerBase
{
    /// <summary>
    /// 前端启动时拉取的公开配置。Google ClientId 会出现在浏览器请求里，本身不是秘密。
    ///
    /// 路径是 /api/v1/common/config。不要在控制器模板里加 [action] ——
    /// 会和方法上的模板拼成 /api/v1/common/config/config。
    /// </summary>
    [HttpGet("config")]
    public ApiResult<AuthConfig> Config()
    {
        return ApiResult<AuthConfig>.Ok(
            new AuthConfig(googleAuthOptions.Value.ClientId));
    }
}
