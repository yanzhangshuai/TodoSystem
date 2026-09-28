using TodoSystem.Web.Utils.Exceptions.Handlers;

namespace TodoSystem.Web.Utils.Exceptions;

public static class CustomExceptionException
{
    public static IServiceCollection AddCustomException(this IServiceCollection services)
    {
        // 注册自定义异常处理
        services.AddExceptionHandler<CustomExceptionHandler>()
            .AddExceptionHandler<SystemExceptionHandler>()
            .AddProblemDetails();

        return services;
    }
}