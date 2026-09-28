using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace TodoSystem.Infrastructure.Token;

public class TokenService(IOptions<TokenOptions> options) : ITokenService
{
    private readonly TokenOptions _token = options.Value;

    public AccessTokenInfo CreateAccessToken(
        int userId,
        string userName,
        string email,
        string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_token.SecretKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var expiresAt = now.AddMinutes(
            _token.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                userId.ToString()),

            new(
                ClaimTypes.Name,
                userName),

            new (
                ClaimTypes.Email, 
                email),
            new(
                ClaimTypes.Role,
                role),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")),
        };

        var token = new JwtSecurityToken(
            issuer: _token.Issuer,
            audience: _token.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new AccessTokenInfo(
            tokenString,
            (int)TimeSpan.FromMinutes(
                _token.AccessTokenMinutes).TotalSeconds);
    }

    public RefreshTokenInfo CreateRefreshToken()
    {
        var token = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

        var expiresAt = DateTime.UtcNow.AddDays(
            _token.RefreshTokenDays);

        return new RefreshTokenInfo(
            token,
            expiresAt);
    }

    public string HashRefreshToken(
        string refreshToken)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToBase64String(bytes);
    }
}