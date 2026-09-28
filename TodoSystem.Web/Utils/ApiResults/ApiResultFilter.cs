using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TodoSystem.Web.Utils.ApiResults;

public class ApiResultFilter: IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        switch (context.Result)
        {
            // 已经是 ApiResult / ApiResult<T>，原样输出，避免二次包装
            case ObjectResult { Value: not null } already when IsApiResult(already.Value):
                await next();
                return;
            // 有返回值成功（return T / Ok(T)）
            case ObjectResult { StatusCode: null or 200 } objectResult:
                context.Result = new ObjectResult(ApiResult.Ok(objectResult.Value!))
                {
                    StatusCode = StatusCodes.Status200OK,
                };
                break;
            // 无返回值成功
            case OkResult:
                context.Result = new ObjectResult(ApiResult.Ok());
                break;
            // 处理 BadRequest
            case BadRequestObjectResult badRequestResult:
                context.Result = new ObjectResult(ApiResult.Fail(StatusCodes.Status400BadRequest, 400, badRequestResult.Value?.ToString() ?? "参数错误"));
                break;
            // 处理 NotFound
            case NotFoundObjectResult notFoundObjectResult:
                context.Result = new ObjectResult(ApiResult.Fail(StatusCodes.Status404NotFound, 404, notFoundObjectResult.Value?.ToString() ?? "Not Found"));
                break;
            // 处理 Unauthorized
            case UnauthorizedResult unauthorizedResult:
                context.Result = new ObjectResult(ApiResult.Fail(StatusCodes.Status401Unauthorized, 401, unauthorizedResult?.ToString() ?? "Unauthorized"));
                break;
            // 处理 Forbidden
            case ForbidResult:
                context.Result = new ObjectResult(ApiResult.Fail(StatusCodes.Status403Forbidden, 403, "Forbidden"));
                break;
        }

        await next();
    }

    private static bool IsApiResult(object value)
    {
        for (var type = value.GetType(); type != null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResult<>))
                return true;
        }
        return false;
    }
}
