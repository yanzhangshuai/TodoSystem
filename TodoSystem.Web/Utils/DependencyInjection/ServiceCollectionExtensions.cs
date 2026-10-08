using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoSystem.Application.Services.Auth;
using TodoSystem.Application.Services.Todo;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Google;
using TodoSystem.Infrastructure.Token;

namespace TodoSystem.Web.Utils.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDependencyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        // 不再校验连接串是否为空：Program.cs 开头的 ValidateRequiredConfiguration()
        // 已经统一拦过。这里只负责把它接到 DbContext 上。
        var dbConnectionString = configuration.GetConnectionString("PgSql")!;

        services.AddDbContext<TodoSystemDbContext>(options =>
        {
            options.UseNpgsql(dbConnectionString);
        });

        services.AddSingleton<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();
        services.AddScoped<ICurrentUser, TodoSystem.Web.Utils.Auth.CurrentUser>();

        services.AddScoped<ITodoListService, TodoListService>();
        services.AddScoped<ITodoItemService, TodoItemService>();
        services.AddScoped<ITagService, TagService>();

        return services;
    }
}
