namespace TodoSystem.Infrastructure.Email;

/// <summary>投递方式取值，对应配置项 <c>Email:Provider</c>。</summary>
public static class EmailProviders
{
    /// <summary>写到本地目录，开发用。</summary>
    public const string File = "File";

    /// <summary>真实 SMTP 发送。</summary>
    public const string Smtp = "Smtp";
}
