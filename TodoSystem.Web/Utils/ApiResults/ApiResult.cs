namespace TodoSystem.Web.Utils.ApiResults;

public class ApiResult<T>
{
    public T? Data { get; set; }
    
    public int StatusCode { get; set; }
    
    public int Code { get; set; }
    
    public string? Message { get; set; }


    public static ApiResult<T> Ok(T data)
    {
        return new ApiResult<T>
        {
            Code = 0,
            Message = "",
            Data = data,
            StatusCode = 200,
        };
    }

    public static ApiResult<T> Error(int statusCode, int code, string message)
    {

        return new ApiResult<T>()
        {
            StatusCode = statusCode,
            Code = code,
            Message = message,
            Data = default
        };
    }
    
    public static ApiResult<T> Error(int code, string message)
    {

        return new ApiResult<T>()
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Code = code,
            Message = message,
            Data = default
        };
    }
}


public class ApiResult : ApiResult<object>
{
    public new static ApiResult Ok()
    {
        return new ApiResult
        {
            Code = 0,
            StatusCode = StatusCodes.Status200OK,
            Data = null,
            Message = ""
        };
    }
    
    public new static ApiResult Fail(int statusCode, int code, string message)
    {
        return new ApiResult
        {
            Code = code,
            StatusCode = statusCode,
            Message = message
        };
    }

    public static ApiResult Fail(int code, string message)
    {
        return new ApiResult
        {
            Code = code,
            StatusCode = StatusCodes.Status500InternalServerError,
            Message = message
        };
    }
}