using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Email;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Infrastructure.Token;

namespace TodoSystem.Application.Services.Auth;

public class EmailVerificationService(
    TodoSystemDbContext db,
    ITokenService tokenService,
    IEmailService emailService,
    IOptions<EmailOptions> options,
    ILogger<EmailVerificationService> logger) : IEmailVerificationService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendVerificationEmailAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                   ?? throw new CustomException(404, "用户不存在");

        if (user.EmailVerifiedAt is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // 旧链接一律作废，只留最新一封。否则用户手上同时有多个有效链接，
        // 点到旧的那个会看到「已失效」，而实际上什么都没做错。
        await db.EmailVerificationTokens
            .Where(x => x.UserId == userId && x.ConsumedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ConsumedAt, now),
                ct);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        db.EmailVerificationTokens.Add(new EmailVerificationTokenEntity
        {
            UserId = userId,
            TokenHash = tokenService.HashToken(token),
            Email = user.Email,
            ExpiresAt = now.AddHours(_options.VerificationTokenHours),
        });

        await db.SaveChangesAsync(ct);

        await emailService.SendAsync(
            new EmailMessage(
                user.Email,
                "验证你的 TodoSystem 邮箱",
                EmailTemplates.VerifyEmail(
                    user.Name,
                    BuildVerifyUrl(token),
                    _options.VerificationTokenHours)),
            ct);
    }

    public async Task<VerifyEmailResult> VerifyAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new CustomException(400, "缺少验证令牌");
        }

        var hash = tokenService.HashToken(token);

        var record = await db.EmailVerificationTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

        if (record is null)
        {
            throw new CustomException(400, "验证链接无效，请重新发送");
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == record.UserId, ct)
                   ?? throw new CustomException(404, "用户不存在");

        if (record.ConsumedAt is not null)
        {
            // 已经验证过就直接返回成功 —— 重复点同一个链接不该显示成失败。
            if (user.EmailVerifiedAt is not null)
            {
                return new VerifyEmailResult(user.Email);
            }

            // 令牌有效但用户还没验证，说明它是被更新的链接顶掉的
            throw new CustomException(400, "这个链接已失效，请用最新一封邮件里的链接");
        }

        if (record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new CustomException(400, "验证链接已过期，请重新发送");
        }

        var now = DateTimeOffset.UtcNow;

        record.ConsumedAt = now;

        // 不覆盖已有的验证时间：先用 Google 验证过、再点旧链接的情况下，
        // 首次验证时间才是要留的那个。
        user.EmailVerifiedAt ??= now;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("用户 {UserId} 完成邮箱验证", user.Id);

        return new VerifyEmailResult(user.Email);
    }

    public async Task ResendAsync(string email, CancellationToken ct = default)
    {
        var normalized = AuthValidation.NormalizeEmail(email);

        if (normalized.Length == 0)
        {
            throw new CustomException(400, "邮箱不能为空");
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalized, ct);

        if (user is null || user.EmailVerifiedAt is not null)
        {
            return;
        }

        // 按 Id 倒序而不是按时间倒序取最新一条：自增主键不受机器时钟回拨影响。
        // 转成可空是为了让「一条都没有」和「时间戳恰好是默认值」能区分开。
        var latestSentAt = await db.EmailVerificationTokens
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.Id)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var cooldown = TimeSpan.FromSeconds(_options.ResendCooldownSeconds);

        if (latestSentAt is { } sentAt && DateTimeOffset.UtcNow - sentAt < cooldown)
        {
            return;
        }

        await SendVerificationEmailAsync(user.Id, ct);
    }

    private string BuildVerifyUrl(string token)
        => $"{_options.WebClientBaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
}
