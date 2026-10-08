namespace TodoSystem.Infrastructure.Token;

public record AccessTokenInfo(
    string Token,
    int ExpiresIn);

/// <summary>
/// Refresh Token 明文与过期时间。
/// ExpiresAt 用 DateTimeOffset：它会直接落到 <c>refresh_tokens.expires_at</c>，
/// 与实体保持同一时区语义，避免 DateTime 因 Kind 不同被写成两种列类型。
/// </summary>
public record RefreshTokenInfo(
    string Token,
    DateTimeOffset ExpiresAt);
