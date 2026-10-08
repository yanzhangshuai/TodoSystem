using TodoSystem.Infrastructure.Email;

namespace TodoSystem.Web.Utils.Email;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddCustomEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailOptions.SectionName);

        services.Configure<EmailOptions>(section);

        // 不给 ?? EmailProviders.File 这样的兜底 —— 那正是「Provider 写错却静默回落
        // File」那个坑。Provider 有没有配、值认不认识，ValidateRequiredConfiguration()
        // 已经在启动时拦过；下面那个 else 分支是防御性的第二道。
        var provider = section.Get<EmailOptions>()?.Provider ?? string.Empty;

        if (string.Equals(provider, EmailProviders.File, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, FileEmailService>();
        }
        else if (string.Equals(provider, EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            // Host / FromAddress 是否为空由 Program.cs 的 ValidateRequiredConfiguration()
            // 统一校验。放这里的话，「哪些键必填」这件事会散在两处，
            // 而且它得先知道 Provider 的值才知道该查哪些键，本来就不该分散。
            services.AddScoped<IEmailService, SmtpEmailService>();
        }
        else
        {
            // 静默回落到 File 会把 "FileSmtp" 这类手滑伪装成「配了但没生效」，
            // 排查成本远高于启动时直接报错。
            throw new InvalidOperationException(
                $"Email:Provider 的值 \"{provider}\" 不认识，" +
                $"只能是 {EmailProviders.File} 或 {EmailProviders.Smtp}。");
        }

        return services;
    }
}
