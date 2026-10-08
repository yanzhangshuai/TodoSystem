namespace TodoSystem.Domain.Constants;

/// <summary>
/// Refresh Token 的吊销原因，落库到 refresh_tokens.revoked_reason。
/// 有了它才能区分「用户主动登出」和「检测到令牌重放」—— 后者是要立刻告警的安全事件。
/// </summary>
public static class RefreshTokenRevocationReasons
{
    /// <summary>用户主动登出。</summary>
    public const string Logout = "logout";

    /// <summary>正常轮换：用旧令牌换到了新令牌。</summary>
    public const string Rotated = "rotated";

    /// <summary>已吊销的令牌被再次使用，判定为泄露，强制该用户全部下线。</summary>
    public const string ReplayDetected = "replay_detected";
}
