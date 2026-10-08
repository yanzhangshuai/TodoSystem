namespace TodoSystem.Infrastructure.Google;

public interface IGoogleTokenValidator
{
    /// <summary>
    /// 校验前端传来的 Google ID Token，返回可信的用户信息。
    /// 校验失败抛出 CustomException(401)。
    /// </summary>
    Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken ct = default);
}
