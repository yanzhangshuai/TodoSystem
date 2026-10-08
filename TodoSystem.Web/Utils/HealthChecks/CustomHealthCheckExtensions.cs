using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TodoSystem.EntityFrameworkCore;

namespace TodoSystem.Web.Utils.HealthChecks;

public static class CustomHealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            // 这个检查是随机返回健康/不健康的，当初用来验证健康检查管道本身通。
            // 接上 HealthDashboard 之后，看板每轮询一次就抛一次硬币，状态一直闪 ——
            // 只关心真实指标的话，把这一段删掉。
            .AddCheck<TestCustomHealthCheck>(
                name: "test_health_check",
                HealthStatus.Unhealthy,
                tags: ["test"]
            )
            .AddUrlGroup(
                new Uri("https://baidu.com"),
                name: "api_health_check",
                HealthStatus.Unhealthy,
                tags: ["baidu"]
            )
            .AddDbContextCheck<TodoSystemDbContext>(
                name: "database_health_check",
                HealthStatus.Unhealthy,
                tags: ["database"]
            );
        return services;
    }

    public static IEndpointRouteBuilder MapCustomHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // 刻意不加 RequireAuthorization：探针拿不到凭据。
        // 负载均衡、容器编排、以及本解决方案里的 TodoSystem.HealthDashboard
        // 都只能裸调这个地址，加了授权它们一律拿 401，然后全部报不健康。
        //
        // 代价是响应里带各检查项的名字和描述，等于对外承认「哪个依赖挂了」。
        // 生产环境要么把它限制在内网，要么改成带密钥的路径再放开。
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        });

        return endpoints;
    }
}