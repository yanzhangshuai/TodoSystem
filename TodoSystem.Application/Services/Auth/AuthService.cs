using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Infrastructure.Token;

namespace TodoSystem.Application.Services.Auth;

public class AuthService(
    TodoSystemDbContext db,
    ITokenService tokenService,
    IPasswordHasher<UserEntity> passwordHasher
    ) : IAuthService
{
    /// <summary>
    /// 验证用户身份。
    ///
    /// 这里只负责：
    /// 1. 查找用户
    /// 2. 验证密码
    /// 3. 验证账号状态
    ///
    /// 不产生 JWT。
    /// </summary>
    public async Task<UserAuthInfo> AuthenticateAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Name == request.UserName || u.Email == request.UserName, ct);

        // 统一提示，避免暴露账号是否存在
        if (user is null)
        {
            throw new CustomException(
                401,
                "用户名或密码错误");
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(
            user,
            user.Password,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new CustomException(
                401,
                "用户名或密码错误");
        }

        if (!user.IsActivated)
        {
            throw new CustomException(
                403,
                "账号不可用");
        }

        return new UserAuthInfo(
            user.Id,
            user.Name,
            user.Email,
            user.Role);
    }

    public async Task<LoginResult> LoginTokenAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await AuthenticateAsync(request, ct);

        return await IssueTokensAsync(user, ct);
    }




    /// <summary>
    /// Refresh Token 换取新的 Access Token + Refresh Token。
    /// </summary>
    public async Task<LoginResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken ct = default)
    {
        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new CustomException( 401, "刷新令牌无效或已过期");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Id == storedToken.UserId, ct);

        if (user is null || !user.IsActivated)
        {
            throw new CustomException( 401, "用户不可用");
        }

        var userAuthInfo = new UserAuthInfo(user.Id, user.Name, user.Email, user.Role);

        // 生成新的 Token
        var result = await IssueTokensAsync(userAuthInfo, ct);

        // Refresh Token Rotation
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = tokenService.HashRefreshToken(result.RefreshToken);

        await db.SaveChangesAsync(ct);

        return result;
    }


    /// <summary>
    /// 注销并吊销 Refresh Token。
    /// </summary>
    public async Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct = default)
    {
        var query = db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null);

        // 指定 Refresh Token
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash =
                tokenService.HashRefreshToken(refreshToken);

            query = query.Where(x => x.TokenHash == tokenHash);
        }

        var tokens = await query.ToListAsync(ct);

        var now = DateTime.UtcNow;

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }


    /// <summary>
    /// 创建 Access Token 和 Refresh Token。
    /// </summary>
    /// <summary>
    /// 创建 Access Token 和 Refresh Token。
    /// </summary>
    private async Task<LoginResult> IssueTokensAsync(
        UserAuthInfo user,
        CancellationToken ct)
    {
        var accessToken =
            tokenService.CreateAccessToken(
                user.UserId,
                user.UserName,
                user.Email,
                user.Role);

        var refreshToken =
            tokenService.CreateRefreshToken();

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.UserId,

            TokenHash =
                tokenService.HashRefreshToken(
                    refreshToken.Token),

            CreatedAt = DateTime.UtcNow,

            ExpiresAt = refreshToken.ExpiresAt
        };

        db.RefreshTokens.Add(refreshTokenEntity);

        await db.SaveChangesAsync(ct);

        return new LoginResult(
            user.UserId,
            user.UserName,
            user.Role,
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresIn,
            refreshToken.ExpiresAt);
    }
}