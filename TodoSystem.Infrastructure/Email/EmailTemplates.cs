namespace TodoSystem.Infrastructure.Email;

/// <summary>
/// 邮件正文模板。目前只有一封验证邮件，所以直接拼字符串，不引模板引擎。
/// </summary>
public static class EmailTemplates
{
    public static string VerifyEmail(string userName, string verifyUrl, int validHours) => $$"""
        <!DOCTYPE html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8">
          <title>验证你的邮箱</title>
        </head>
        <body style="margin:0;padding:24px;background:#f7f7f6;font-family:-apple-system,'Segoe UI','Microsoft YaHei',sans-serif;color:#1c1c1a">
          <div style="max-width:480px;margin:0 auto;background:#ffffff;border:1px solid #e2e2dd;border-radius:10px;padding:28px">
            <h1 style="font-size:18px;font-weight:600;margin:0 0 14px">验证你的邮箱</h1>

            <p style="font-size:14px;line-height:1.7;margin:0 0 20px;color:#5c5c58">
              {{userName}}，欢迎使用 TodoSystem。点下面的按钮完成邮箱验证。
            </p>

            <a href="{{verifyUrl}}" style="display:inline-block;background:#1c1c1a;color:#ffffff;text-decoration:none;padding:10px 20px;border-radius:8px;font-size:14px">验证邮箱</a>

            <p style="font-size:13px;line-height:1.7;margin:22px 0 0;color:#8a8a84">
              链接 {{validHours}} 小时内有效，只能使用一次。<br>
              按钮点不开的话，把下面这个地址复制到浏览器：
            </p>

            <p style="font-size:12px;line-height:1.6;word-break:break-all;margin:8px 0 0;color:#5c5c58">{{verifyUrl}}</p>

            <p style="font-size:12px;line-height:1.7;margin:22px 0 0;color:#8a8a84">
              不是你本人注册的？忽略这封邮件即可，账号不会被激活。
            </p>
          </div>
        </body>
        </html>
        """;
}
