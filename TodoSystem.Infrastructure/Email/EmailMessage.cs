namespace TodoSystem.Infrastructure.Email;

/// <summary>一封待发邮件。只带 HTML 正文 —— 本项目发的都是排版过的通知。</summary>
public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody);
