using System.Security.Claims;
using TodoSystem.Application.Services.Auth;

namespace TodoSystem.Web.Utils.Auth;

/// <summary>
/// 从 HttpContext.User 解析当前用户。JWT / Cookie 两种方案都落到同一套 Claims。
/// </summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int Id =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
