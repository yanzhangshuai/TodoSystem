using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Web.Utils.Exceptions.Handlers;

public class SystemExceptionHandler(ILogger<CustomException> logger, IWebHostEnvironment environment): IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is CustomException) return false;

        logger.LogError(
            exception, "Exceptions occurred: {Message} {StackTrace} {Source}", exception.Message, exception.StackTrace,
            exception.Source);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "This is System Exception",
            Detail = environment.IsDevelopment() ? $"Exceptions occurred: {exception.Message} {exception.StackTrace} {exception.Source}" : $"Exceptions occurred: {exception.Message}"
        };


        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}