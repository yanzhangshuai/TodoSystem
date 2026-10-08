namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// 邮件配置。绑定 <c>Email</c> 节点。
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// 投递方式，取值见 <see cref="EmailProviders"/>。
    /// </summary>
    /// <remarks>
    /// 刻意没有默认值。默认成 <see cref="EmailProviders.File"/> 的后果是
    /// 「以为在发信，其实一封都没发出去」，要等第一个用户注册才暴露。
    /// 「必须显式配」这件事由启动期的 ValidateRequiredConfiguration() 保证。
    /// </remarks>
    public string Provider { get; set; } = string.Empty;

    /// <remarks>
    /// 同样刻意没有默认值。填一个 no-reply@todosystem.local 会让 SMTP 服务器
    /// 以 553 拒收（163 要求发件地址和认证用户完全一致），
    /// 而那个报错里完全看不出是配置的问题。
    /// </remarks>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "TodoSystem";

    /// <summary>
    /// 前端地址，不是后端地址。验证链接指向前端页面，后端只提供换取结果的接口 ——
    /// 这样链接失效时能渲染成页面上的提示，而不是一屏 JSON。
    /// </summary>
    /// <remarks>
    /// 没有默认值。http://localhost:5173 只在开发环境成立，写在这里的话
    /// 生产漏配就会生成指向 localhost 的死链接 —— 又一个静默失效。
    /// </remarks>
    public string WebClientBaseUrl { get; set; } = string.Empty;

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

    /// <summary>
    /// 是否启用 STARTTLS（<c>SmtpClient.EnableSsl</c>）。
    /// 只对 STARTTLS 有意义 —— 465 / 994 是「连上就是 TLS」的隐式 TLS，
    /// <c>SmtpClient</c> 不支持，配 false 也不等于能用，代码里会直接拦下。
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
