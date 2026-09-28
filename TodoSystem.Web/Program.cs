using Asp.Versioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Web.Utils.ApiResults;
using TodoSystem.Web.Utils.Auth;
using TodoSystem.Web.Utils.DependencyInjection;
using TodoSystem.Web.Utils.Exceptions;
using TodoSystem.Web.Utils.HealthChecks;
using TodoSystem.Web.Utils.Logging;
using TodoSystem.Web.Utils.Route;


#region builder
var builder = WebApplication.CreateBuilder(args);

// 日志写入
builder.AddLoggerProvider();

builder.Services.AddControllers()
    .AddMvcOptions(options =>
    {
        
        options.Filters.Add<ApiResultFilter>();
        // 路由转小写
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
    .AddDependencyInjection(builder.Configuration)
    .AddCustomHealthChecks();


#endregion


#region app
var app = builder.Build();
app.UseExceptionHandler()
    .UseCustomAuth();

// 空库时写入联调种子账号（ADMIN/USER，密码 123）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoSystemDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Users.AnyAsync())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();
        var admin = new UserEntity { Name = "ADMIN", Email = "admin@test.local", Role = "ADMIN", IsActivated = true };
        admin.Password = hasher.HashPassword(admin, "123");
        var user = new UserEntity { Name = "USER", Email = "user@test.local", Role = "USER", IsActivated = true };
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
#endregion


