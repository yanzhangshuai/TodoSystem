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
    /// 令牌明文 → 存储用哈希（Base64(SHA-256)）。
    /// Refresh Token 和邮箱验证令牌共用同一种哈希，所以名字不绑定某一种令牌。
    /// </summary>
    string HashToken(string token);
}