using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// 模拟发送邮件
/// </summary>
public class FileEmailService(
    IOptions<EmailOptions> options,
    ILogger<FileEmailService> logger) : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var directory = Path.GetFullPath(_options.FileOutputDirectory);
        Directory.CreateDirectory(directory);

        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff");
        var path = Path.Combine(directory, $"{stamp}-{ToFileName(message.To)}.html");

        await File.WriteAllTextAsync(path, message.HtmlBody, ct);

        logger.LogInformation("邮件已落盘（Email:Provider=File）：{Path} → {To}", path, message.To);

        // 正文里的第一个链接就是要点的那个，单独打出来省得去翻文件
        if (ExtractFirstLink(message.HtmlBody) is { } link)
        {
            logger.LogInformation("邮件链接：{Link}", link);
        }
    }

    private static string ToFileName(string address)
    {
        var invalid = Path.GetInvalidFileNameChars();

        return string.Concat(address.Select(c => invalid.Contains(c) ? '_' : c));
    }

    /// <summary>
    /// 用字符串查找而不是正则取第一个 href：模板是我们自己写的，
    /// 结构固定，正则反而多一层需要维护的语法。
    /// </summary>
    private static string? ExtractFirstLink(string html)
    {
        const string marker = "href=\"";

        var start = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = html.IndexOf('"', start);

        return end < 0 ? null : html[start..end];
    }
}
