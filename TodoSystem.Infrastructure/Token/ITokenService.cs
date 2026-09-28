namespace TodoSystem.Infrastructure.Token;

public interface ITokenService
{
    /// <summary>
    /// 签发短期 Access Token（JWT）
    /// </summary>
    AccessTokenInfo CreateAccessToken(
        int userId,
        string userName,
        string email,
        string role);

    /// <summary>
    /// 生成不透明 Refresh Token。
    /// </summary>
    RefreshTokenInfo CreateRefreshToken();

    /// <summary>
    /// Refresh Token 明文 → 存储用哈希
    /// </summary>
    string HashRefreshToken(string refreshToken);
}