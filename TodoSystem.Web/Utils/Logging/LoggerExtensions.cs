using Serilog;

namespace TodoSystem.Web.Utils.Logging;

public static class LoggerExtensions
{
    public static WebApplicationBuilder AddLoggerProvider(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();
        
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog();

        return builder;
    }
}