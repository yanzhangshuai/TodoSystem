namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// 邮件服务
/// </summary>
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
