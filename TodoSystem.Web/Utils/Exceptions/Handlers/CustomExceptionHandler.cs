using Microsoft.AspNetCore.Diagnostics;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Web.Utils.ApiResults;

namespace TodoSystem.Web.Utils.Exceptions.Handlers;

public class CustomExceptionHandler(ILogger<CustomException> logger): IExceptionHandler
{
    /// <summary>
    /// 返回 true 表示已处理并结束；返回 false 交给下一个 handler。
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
