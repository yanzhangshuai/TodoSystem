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
        var dbConnectionString = configuration.GetConnectionString("PgSql")
                                 ?? throw new InvalidOperationException("未配置数据库连接字符串");

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
