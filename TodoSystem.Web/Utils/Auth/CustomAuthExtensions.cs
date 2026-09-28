using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Infrastructure.Token;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Utils.Auth;

public static class CustomAuthExtensions
{
    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TokenOptions>(configuration.GetSection(TokenOptions.SectionName));
        
        var jwt = configuration.GetSection(TokenOptions.SectionName).Get<TokenOptions>()
                  ?? throw new CustomException(500, "未配置 JWT");

        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || Encoding.UTF8.GetByteCount(jwt.SecretKey) < 32)
            throw new CustomException(500, "JWT SecretKey 至少 32 字节");

        services.AddHttpContextAccessor();

        // 默认方案按请求头转发：Bearer → JWT，否则 Cookie
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
            
            // google
            .AddGoogleOpenIdConnect(options =>
            {
                // 从配置文件读取Google的ClientId和ClientSecret
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                // 回调路径要和Google开发者控制台里配置的完全一致
                options.CallbackPath = "/api/auth/google-callback";
                // 可选：自定义映射用户信息到Claims
                // options.ClaimActions.MapJsonKey("urn:google:avatar", "picture", "url");
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
