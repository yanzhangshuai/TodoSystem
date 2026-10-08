using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Email;
using TodoSystem.Web.Utils.ApiResults;
using TodoSystem.Web.Utils.Auth;
using TodoSystem.Web.Utils.Configuration;
using TodoSystem.Web.Utils.DependencyInjection;
using TodoSystem.Web.Utils.Email;
using TodoSystem.Web.Utils.Exceptions;
using TodoSystem.Web.Utils.HealthChecks;
using TodoSystem.Web.Utils.Logging;
using TodoSystem.Web.Utils.Route;

var builder = WebApplication.CreateBuilder(args);

// 配置体检放在最前面 —— 必须在任何 DI 注册、任何 IOptions 绑定之前。
// 排在后面的话，DI 阶段先抛出的异常会把真正的原因盖掉（比如那句
// 「未配置数据库连接字符串」会先炸，后面「JWT 密钥也没配」就看不到了）。
builder.ValidateRequiredConfiguration();

builder.AddLoggerProvider();

builder.Services.AddControllers()
    .AddMvcOptions(options =>
    {
        options.Filters.Add<ApiResultFilter>();

        // 路由参数转小写
        options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));

        options.Conventions.Add(
            new GlobalRoutePrefixConvention(
                new RouteAttribute("api")));
    });


builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true; // 强制全URL小写
        options.LowercaseQueryStrings = true; // 查询参数小写
        options.AppendTrailingSlash = false; // 移除末尾斜杠
    })
    .AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true; // 如果没有请求声明，使用默认版本
        options.DefaultApiVersion = new ApiVersion(1, 0); // 默认版本
        options.ReportApiVersions = true; //在Header 返回支持的版本
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddOpenApi("v1");
builder.Services.AddOpenApi("v2");


builder.Services.AddCustomException()
    .AddCustomAuth(builder.Configuration)
    .AddCustomEmail(builder.Configuration)
    .AddDependencyInjection(builder.Configuration)
    .AddCustomHealthChecks();


var app = builder.Build();

app.UseExceptionHandler()
    .UseCustomAuth();

// 非开发环境用 File 投递：邮件只写文件，不会真的寄出。
// 不拦下来（演示 / 内网是合法用法），但必须留下痕迹 ——
// 否则就成了「以为在发信，其实一封都没发出去」，要等第一个用户注册才暴露。
if (!app.Environment.IsDevelopment()
    && string.Equals(builder.Configuration["Email:Provider"], EmailProviders.File,
        StringComparison.OrdinalIgnoreCase))
{
    app.Logger.LogWarning(
        "Email:Provider = File（环境 {Environment}）：邮件只落盘、不会真的寄出。要真发信请改成 Smtp 并配好 SMTP 凭据。",
        app.Environment.EnvironmentName);
}


// 空库时写入联调种子账号（ADMIN/USER，密码 123）。
// 默认只在开发环境写 —— 生产库首次启动如果照写，等于凭空建两个弱口令管理员账号。
// 要在别的环境也写，用环境变量 Seed__Enabled=true 显式打开。
var seedEnabled = builder.Configuration.GetValue<bool?>("Seed:Enabled")
                  ?? app.Environment.IsDevelopment();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoSystemDbContext>();
    await db.Database.MigrateAsync();

    if (seedEnabled && !await db.Users.AnyAsync())
    {
        if (!app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning("正在非开发环境写入种子账号 ADMIN/USER（密码 123），上线前记得改密码或删掉");
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();

        // EmailVerifiedAt 保持 null：种子账号是本地注册的，邮箱从未验证过。
        // 这是刻意的 —— 未验证邮箱不能被第三方登录自动合并。
        var admin = new UserEntity
        {
            Name = "ADMIN",
            Email = "admin@test.local",
            Role = UserRoles.Admin,
            IsActivated = true
        };
        admin.Password = hasher.HashPassword(admin, "123");

        var user = new UserEntity
        {
            Name = "USER",
            Email = "user@test.local",
            Role = UserRoles.User,
            IsActivated = true
        };
        user.Password = hasher.HashPassword(user, "123");

        db.Users.AddRange(admin, user);
        await db.SaveChangesAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .AddDocument("v1", "TodoSystem API v1")
            .AddDocument("v2", "TodoSystem API v2");
    });
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.MapCustomHealthChecks();

app.Run();
