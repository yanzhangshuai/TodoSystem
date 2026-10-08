using System.Text;
using TodoSystem.Infrastructure.Email;

namespace TodoSystem.Web.Utils.Configuration;

/// <summary>
/// 启动期的配置体检：把所有「缺了就起不来」的配置项一次查完，再一次性报出来。
/// </summary>
/// <remarks>
/// 为什么单独有这一层，而不是让每个扩展方法自己抛：
///
/// 1. <b>一次报全部，而不是一次报一个。</b>配置错误是「部署时」的错误，
///    分散在各处的结果是：补上连接串 → 重启 → 发现 JWT 密钥也没配 → 再重启 → ……
///    本来一轮就能改完的事要重启好几遍。
///
/// 2. <b>把「运行时才炸」提前到「启动就炸」。</b>改之前至少有两处是延迟爆雷：
///    <c>Email:Smtp:Host</c> 没配要等第一个用户注册才抛异常；
///    <c>Jwt:Issuer</c> 被覆盖成空串要等第一个带令牌的请求才 401。
///    都属于「以为配好了，其实没有」—— 和当初 Provider 写错静默回落 File 是同一类问题。
///
/// 3. <b>必须跑在 Program.cs 最前面。</b>排在后面的话，DI 注册阶段先抛出的异常
///    会把真正的原因盖掉，人只看到最后那一层。
///
/// 校验的是「配置存不存在、组合起来自不自洽」，不是「值对不对」——
/// 密码错、端口不通，那要等真去连才知道。
/// </remarks>
public static class ConfigurationValidationExtensions
{
    public static WebApplicationBuilder ValidateRequiredConfiguration(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var problems = new List<string>();

        ValidateDatabase(config, problems);
        ValidateJwt(config, problems);
        ValidateEmail(config, problems);

        if (problems.Count > 0)
        {
            var lines = new List<string>
            {
                $"配置不完整，启动中止（共 {problems.Count} 项）。当前环境：{builder.Environment.EnvironmentName}",
                string.Empty
            };

            lines.AddRange(problems.Select((problem, index) => $"  {index + 1}. {problem}"));

            lines.Add(string.Empty);
            lines.Add("逐项的「在哪配、缺了会怎样」见 README 的「配置总览」一节。");

            throw new InvalidOperationException(string.Join(Environment.NewLine, lines));
        }

        return builder;
    }

    private static void ValidateDatabase(IConfiguration config, List<string> problems)
    {
        if (!string.IsNullOrWhiteSpace(config.GetConnectionString("PgSql")))
        {
            return;
        }

        problems.Add(
            "ConnectionStrings:PgSql —— 数据库连接串是空的。\n" +
            "      本机开发：appsettings.Development.json。\n" +
            "      容器部署：不用手写，compose 会用 .env 里的 POSTGRES_* 拼出来。");
    }

    private static void ValidateJwt(IConfiguration config, List<string> problems)
    {
        var secretKey = config["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            problems.Add(
                "Jwt:SecretKey —— 没配。没有它签不出令牌，也验不了令牌。\n" +
                "      本机开发：appsettings.Development.json。\n" +
                "      容器部署：.env 里的 JWT_SECRET_KEY（openssl rand -base64 48 生成）。");
        }
        else
        {
            // 密钥短于 256 位时 SymmetricSecurityKey 不是启动就报，
            // 而是等第一次签发令牌才抛 —— 又一个「启动看着挺好」的坑。
            var byteCount = Encoding.UTF8.GetByteCount(secretKey);
            if (byteCount < 32)
            {
                problems.Add(
                    $"Jwt:SecretKey —— 只有 {byteCount} 字节，HMAC-SHA256 要求至少 32 字节。\n" +
                    "      用 openssl rand -base64 48 重新生成一个。");
            }
        }

        // Issuer / Audience 在 appsettings.json 里有默认值，正常不会缺。
        // 但 compose 里一个手滑的 Jwt__Issuer= 就会把它们覆盖成空串，
        // 而 ValidateIssuer = true 配上空的 ValidIssuer，后果是每个带令牌的
        // 请求都 401（IDX10204）—— 要等到有人登录了才发现。
        RequireNonEmpty(config, "Jwt:Issuer", "JWT 签发方标识", problems);
        RequireNonEmpty(config, "Jwt:Audience", "JWT 接收方标识", problems);
    }

    private static void ValidateEmail(IConfiguration config, List<string> problems)
    {
        var provider = config["Email:Provider"];
        var isFile = string.Equals(provider, EmailProviders.File, StringComparison.OrdinalIgnoreCase);
        var isSmtp = string.Equals(provider, EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(provider))
        {
            // 刻意不给默认值：默认 File 会变成「以为在发信，其实一封都没发出去」，
            // 默认 Smtp 又会在没配 SMTP 时启动失败。两个默认都不合适，所以要求显式写。
            problems.Add(
                $"Email:Provider —— 没配。必须显式写 {EmailProviders.File}（邮件落盘）" +
                $"或 {EmailProviders.Smtp}（真发信）。");
        }
        else if (!isFile && !isSmtp)
        {
            // 静默回落会把 "FileSmtp" 这种手滑伪装成「配了但没生效」，
            // 排查成本远高于启动时直接报错。
            problems.Add(
                $"Email:Provider —— 值 \"{provider}\" 不认识，" +
                $"只能是 {EmailProviders.File} 或 {EmailProviders.Smtp}。");
        }

        if (isSmtp)
        {
            RequireNonEmpty(config, "Email:Smtp:Host", "SMTP 服务器地址，163 是 smtp.163.com", problems);

            // FromAddress 为空时 MailAddress 的构造会抛 FormatException，
            // 报错信息里完全看不出是邮件配置的问题。
            RequireNonEmpty(config, "Email:FromAddress", "发件地址", problems);

            ValidateSmtpCredentials(config, problems);
        }

        ValidateWebClientBaseUrl(config, problems);
    }

    private static void ValidateSmtpCredentials(IConfiguration config, List<string> problems)
    {
        var userName = config["Email:Smtp:UserName"];
        var password = config["Email:Smtp:Password"];

        // 两个都没配是允许的：内网中继可能本来就不需要认证。
        // 只配一个则是漏了 —— 结局一定是 535 认证失败，不如现在就说。
        if (!string.IsNullOrWhiteSpace(userName) && string.IsNullOrWhiteSpace(password))
        {
            problems.Add(
                "Email:Smtp:Password —— 配了 Email:Smtp:UserName 却没配密码，是漏了一项。\n" +
                "      163 用的是「授权码」不是登录密码，去邮箱设置里生成。");
        }
        else if (string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
        {
            problems.Add("Email:Smtp:UserName —— 配了密码却没配用户名，是漏了一项。");
        }
    }

    private static void ValidateWebClientBaseUrl(IConfiguration config, List<string> problems)
    {
        var baseUrl = config["Email:WebClientBaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            problems.Add(
                "Email:WebClientBaseUrl —— 没配。验证邮件里的链接是拿它拼的，\n" +
                "      为空会生成 /verify-email?token=… 这样的相对地址，用户点开是死链接。\n" +
                "      本机开发：http://localhost:5173。\n" +
                "      容器部署：.env 里的 PUBLIC_BASE_URL，要指向前端入口而不是 API。");
            return;
        }

        // 配错不会报错，只会生成打不开的链接 —— 典型的静默失效，
        // 所以顺手验成合法的 http/https 绝对地址。
        var isHttpUrl = Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri)
                        && uri is not null
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isHttpUrl)
        {
            problems.Add(
                $"Email:WebClientBaseUrl —— \"{baseUrl}\" 不是合法的 http/https 绝对地址。\n" +
                "      要带协议头，例如 http://localhost:5173 或 https://example.com。");
        }
    }

    private static void RequireNonEmpty(
        IConfiguration config,
        string key,
        string what,
        List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            problems.Add($"{key} —— 没配（{what}）。");
        }
    }
}
