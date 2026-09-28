using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TodoSystem.Web.Utils.HealthChecks;

public class TestCustomHealthCheck: IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        
        // 这是一个测试 健康检查，随机true 或 false
        var isHealthy = Random.Shared.Next(2) == 0;

        if (isHealthy)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("这是一个测试成功的健康检查！！！"));
        }

        return Task.FromResult(
            new HealthCheckResult(
                context.Registration.FailureStatus, "这是一个测试失败的健康检查！！！"));
    }
}