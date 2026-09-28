namespace TodoSystem.Infrastructure.Exceptions;

/// <summary>
/// 业务异常。Code 对应 HTTP 状态码或业务码，由 Web 层统一转成响应。
/// </summary>
public class CustomException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;

    public override string Message { get; } = message;
}
