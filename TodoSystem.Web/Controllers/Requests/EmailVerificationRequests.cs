namespace TodoSystem.Web.Controllers.Requests;

/// <summary>
/// 邮箱验证入参。令牌放请求体而不是查询串 —— 查询串会进访问日志和 Referer，
/// 而这是一个一次性的凭证。
/// </summary>
public class VerifyEmailInput
{
    public string Token { get; set; } = default!;
}

/// <summary>重发验证邮件入参。</summary>
public class ResendVerificationInput
{
    public string Email { get; set; } = default!;
}
