using TodoSystem.Application.Services.Auth.Dtos;

namespace TodoSystem.Application.Services.Auth;

/// <summary>
/// 邮箱验证。与凭证方案无关 —— 用户是从邮件里点进来的，那时未必有会话，
/// 所以这里的动作都不依赖「当前登录用户」。
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// 给指定用户发验证邮件。会先作废该用户此前尚未失效的令牌，
    /// 保证同时只有最新一封邮件里的链接可用。
    /// </summary>
    Task SendVerificationEmailAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// 消费令牌并回填 <c>EmailVerifiedAt</c>。
    /// 同一个链接重复验证视为成功（幂等）：浏览器预取、React StrictMode 双跑、
    /// 用户手动刷新都会让这个请求发不止一次。
    /// </summary>
    Task<VerifyEmailResult> VerifyAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// 按邮箱重发。邮箱不存在、已验证、或还在冷却期内，一律静默返回成功 ——
    /// 这个端点匿名可调，响应上的任何差异都会变成账号探测手段。
    /// </summary>
    Task ResendAsync(string email, CancellationToken ct = default);
}
