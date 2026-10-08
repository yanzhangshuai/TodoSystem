using Microsoft.AspNetCore.Mvc;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Web.Controllers.v1;

/// <summary>
/// 待办领域控制器的公共基类，只提供「取当前用户 Id」这一个能力。
///
/// 刻意不带任何 MVC 特性：[ApiVersion] 之类的特性由 Asp.Versioning 从控制器自身的
/// ControllerModel.Attributes 上读取，放在抽象基类上能否被继承取决于框架实现细节。
/// 版本号是接口契约，不值得为省三行代码去赌，写显式的最稳。
/// </summary>
public abstract class TodoControllerBase(ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// 控制器都挂了 [Authorize]，取不到只可能是令牌里没有可解析的用户标识 —— 那本身就是未登录。
    /// </summary>
    protected int CurrentUserId => currentUser.Id > 0
        ? currentUser.Id
        : throw new CustomException(401, "未登录");
}
