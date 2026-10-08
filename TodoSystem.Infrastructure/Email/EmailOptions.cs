namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// 邮件配置。绑定 <c>Email</c> 节点。
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>投递方式，取值见 <see cref="EmailProviders"/>。</summary>
    public string Provider { get; set; } = EmailProviders.File;

    public string FromAddress { get; set; } = "no-reply@todosystem.local";

    public string FromName { get; set; } = "TodoSystem";

    /// <summary>
    /// 前端地址，不是后端地址。验证链接指向前端页面，后端只提供换取结果的接口 ——
    /// 这样链接失效时能渲染成页面上的提示，而不是一屏 JSON。
    /// </summary>
    public string WebClientBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>验证链接有效期（小时）。</summary>
    public int VerificationTokenHours { get; set; } = 24;

    /// <summary>
    /// 同一账号两次重发之间的最小间隔（秒）。没有这一条，
    /// 匿名可调的重发接口就是一个现成的邮件轰炸器。
    /// </summary>
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary><see cref="EmailProviders.File"/> 模式下的输出目录，相对后端进程的工作目录。</summary>
    public string FileOutputDirectory { get; set; } = "emails";

    public SmtpOptions Smtp { get; set; } = new();
}

/// <summary>SMTP 连接参数，只有 <c>Provider = Smtp</c> 时才会读到。</summary>
public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>587 端口配 STARTTLS 填 true；465 端口是直接 TLS，要填 false。</summary>
    public bool UseStartTls { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
