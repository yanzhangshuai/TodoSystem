using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using TodoSystem.Infrastructure.Exceptions;

namespace TodoSystem.Infrastructure.Google;

/// <summary>
/// 用 Google 官方库校验 ID Token 的签名、签发方、有效期和 aud。
/// 校验通过后 payload 内容才可信，绝不能直接信任前端传来的邮箱。
/// </summary>
public class GoogleTokenValidator(IOptions<GoogleAuthOptions> options) : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options = options.Value;

    public async Task<GoogleUserPayload> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new CustomException(400, "缺少 Google 凭证");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new CustomException(500, "未配置 Google ClientId");
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    // 关键：audience 必须等于本应用的 ClientId，
                    // 否则别人应用的 id_token 也能拿来登录。
                    Audience = new[] { _options.ClientId },
                });
        }
        catch (InvalidJwtException)
        {
            throw new CustomException(401, "Google 凭证无效或已过期");
        }
        catch (ArgumentException)
        {
            throw new CustomException(401, "Google 凭证格式不正确");
        }

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            throw new CustomException(401, "Google 未返回账号标识");
        }

        return new GoogleUserPayload(
            payload.Subject,
            payload.Email,
            payload.EmailVerified,
            payload.Name,
            payload.Picture,
            payload.Locale);
    }
}
