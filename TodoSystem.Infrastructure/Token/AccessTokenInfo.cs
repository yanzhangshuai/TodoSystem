namespace TodoSystem.Infrastructure.Token;

public record AccessTokenInfo(
    string Token,
    int ExpiresIn);

public record RefreshTokenInfo(
    string Token,
    DateTime ExpiresAt);