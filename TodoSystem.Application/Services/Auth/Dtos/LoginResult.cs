namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>登录/刷新成功后的令牌包。AccessToken 给 API，RefreshToken 用于续期。</summary>
public record LoginResult(
    int UserId,
    string UserName,
    string Role,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    DateTime RefreshTokenExpiresAt);
