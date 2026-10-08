using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// SMTP 投递。用 System.Net.Mail.SmtpClient 而不是 MailKit：官方已不建议新项目用它，
/// 但这里只发纯通知邮件，为它多引两个第三方包不划算。
/// </summary>
public class SmtpEmailService(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var smtp = _options.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException(
                "Email:Provider 选了 Smtp，但没有配置 Email:Smtp:Host");
        }

        // SmtpClient 的 EnableSsl 走的是 RFC 3207 的 STARTTLS —— 明文连上去再升级。
        // 465 / 994 是「连上就是 TLS」，握手内容对不上，表现是干等到超时，
        // 报错信息里看不出跟端口有关。几乎所有中文教程都写 465，所以这里必须拦住。
        if (smtp.Port is 465 or 994)
        {
            throw new InvalidOperationException(
                $"System.Net.Mail.SmtpClient 不支持 {smtp.Port} 端口的隐式 TLS。" +
                "163 改用 25 端口 + STARTTLS（UseStartTls = true）；" +
                "一定要走 465 就只能换成 MailKit。");
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.UseStartTls,
            Credentials = string.IsNullOrWhiteSpace(smtp.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(smtp.UserName, smtp.Password),
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
        };

        mail.To.Add(message.To);

        await client.SendMailAsync(mail, ct);

        logger.LogInformation("邮件已通过 SMTP 发出：{To} / {Subject}", message.To, message.Subject);
    }
}
