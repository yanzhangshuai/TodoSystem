using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Web.Controllers.v1;

namespace TodoSystem.Web.Utils.Auth;

/// <summary>
/// Cookie 凭证方案的会话建立与销毁，只有 v1
/// （<see cref="CookieAuthController"/>）用得到。
/// </summary>
public static class CookieSignInExtensions
{
    /// <summary>
    /// 用 Cookie 方案建立会话。
    ///
    /// Claims 与 JWT 方案保持一致（NameIdentifier / Name / Email / Role），
    /// 这样 <see cref="CurrentUser"/>、<c>[Authorize(Roles = ...)]</c>
    /// 对两种方案是同一套逻辑，不需要分支判断。
    /// </summary>
    public static Task SignInWithCookieAsync(this HttpContext http, UserAuthInfo user)
    {
        var identity = new ClaimsIdentity(
            CookieAuthenticationDefaults.AuthenticationScheme);

        identity.AddClaims(
        [
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        ]);

        return http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                // 持久 Cookie，与 CustomAuthExtensions 里 Cookie 的
                // ExpireTimeSpan（7 天）+ SlidingExpiration 配合
                IsPersistent = true
            });
    }

    /// <summary>清除 Cookie 会话。</summary>
    public static Task SignOutCookieAsync(this HttpContext http)
        => http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
