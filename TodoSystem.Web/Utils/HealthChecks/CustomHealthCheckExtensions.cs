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
        endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
        })
        .RequireAuthorization();
        return endpoints;
    }
}