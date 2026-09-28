using Microsoft.AspNetCore.Diagnostics;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Utils.Exceptions.Handlers;

public class CustomExceptionHandler(ILogger<CustomException> logger): IExceptionHandler
{
    /// <summary>
    /// 异常拦截器 return true 结束， false 到下一个 ExceptionHandler。 找不到就默认UseExceptionHandler处理
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not CustomException customException) return false;

        logger.LogError(
            exception, "Exceptions occurred: {Message} {StackTrace} {Source}", exception.Message, exception.StackTrace, exception.Source);

        httpContext.Response.StatusCode = customException.Code;
        await httpContext.Response.WriteAsJsonAsync(
            ApiResult.Fail(customException.Code, customException.Code, customException.Message),
            cancellationToken);

        return true;
    }
}
