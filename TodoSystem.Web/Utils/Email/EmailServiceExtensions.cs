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

        var provider = section.Get<EmailOptions>()?.Provider ?? EmailProviders.File;

        if (string.Equals(provider, EmailProviders.File, StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailService, FileEmailService>();
        }
        else if (string.Equals(provider, EmailProviders.Smtp, StringComparison.OrdinalIgnoreCase))
        {
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
