using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services
    .AddHealthChecksUI(options =>
    {
        // 端点写在配置里而不是硬编码：本机跑和容器里跑，API 的地址不一样。
        foreach (var endpoint in builder.Configuration.GetSection("HealthCheckEndpoints").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(endpoint.Value))
            {
                options.AddHealthCheckEndpoint(endpoint.Key, endpoint.Value);
            }
        }

        // 30 秒轮询一次；失败通知最短 60 秒一条，避免一个持续故障把通知刷爆。
        // 这两个是 Settings 上的扩展方法 —— 不能挂到 AddInMemoryStorage() 后面，
        // 那个返回的是 HealthChecksUIBuilder，编译不过（试过）。
        options.SetEvaluationTimeInSeconds(30)
            .SetMinimumSecondsBetweenFailureNotifications(60);
    })
    .AddInMemoryStorage();

var app = builder.Build();

// 没开 UseHttpsRedirection：这个看板只绑本机/内网，没配 HTTPS 端口时
// 那个中间件会每次请求都打一条「无法确定 https 端口」的警告。
// 真要对外暴露，TLS 应该终结在反向代理上。
app.MapHealthChecksUI(options => options.UIPath = "/dashboard");

app.Run();
