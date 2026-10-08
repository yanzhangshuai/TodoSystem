namespace TodoSystem.Infrastructure.Google;

/// <summary>
/// 校验通过的 Google 用户信息。
/// 只保留业务需要的字段，避免上层依赖 Google SDK 的类型。
/// </summary>
/// <param name="Subject">Google 用户唯一标识（sub），不可变，作为绑定键。</param>
public record GoogleUserPayload(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? Name,
    string? Picture,
    string? Locale);
