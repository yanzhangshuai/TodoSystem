using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TodoSystem.Infrastructure.Google;
using TodoSystem.Infrastructure.Token;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Utils.Auth;

public static class CustomAuthExtensions
{
    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Google 登录配置（ClientId / ClientSecret）
        services.Configure<GoogleAuthOptions>(
            configuration.GetSection(GoogleAuthOptions.SectionName));

        // JWT
        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));

        // 这里不再校验 SecretKey 的长度和 Issuer / Audience 是否为空：
        // Program.cs 开头的 ValidateRequiredConfiguration() 已经统一拦过，
        // 而且是把所有问题一次报完。两处都写，加新配置项时一定会漏掉一边。
        var jwt = configuration.GetSection(TokenOptions.SectionName).Get<TokenOptions>()!;

        services.AddHttpContextAccessor();

        // 默认方案按请求头转发：Bearer → JWT，否则 Cookie
        //
        // 注意：Google 登录走的是「前端拿 id_token → POST 到 /api/auth/google」的
        // ID Token 直传流程，由 IGoogleTokenValidator 验签，不经过这里。
        // 因此不再注册 Google 的 OIDC 重定向方案。
        services.AddAuthentication(AuthSchemes.BearerOrCookie)
            .AddPolicyScheme(AuthSchemes.BearerOrCookie, "Bearer or Cookie", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var header = context.Request.Headers.Authorization.ToString();
                    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };

                options.Events = new JwtBearerEvents
                {
                    // 统一 401，和 ApiResult 错误结构对齐
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            ApiResult.Fail(StatusCodes.Status401Unauthorized, 401, "未登录或登录已过期"));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            ApiResult.Fail(StatusCodes.Status403Forbidden, 403, "无权限"));
                    },
                };
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = "AUTH";
                options.LoginPath = "/account/login";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // 前后端分离，Cookie 需要跨端口携带
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;

                // 未登录 401 / 无权限 403，不走重定向（API 友好）
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .SetInvokeHandlersAfterFailure(true);

        return services;
    }

    public static IApplicationBuilder UseCustomAuth(this IApplicationBuilder app)
    {
        app.UseAuthentication()
            .UseAuthorization();
        return app;
    }
}
