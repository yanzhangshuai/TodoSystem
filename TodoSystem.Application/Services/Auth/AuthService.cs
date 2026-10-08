using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TodoSystem.Application.Common;
using TodoSystem.Application.Services.Auth.Dtos;
using TodoSystem.Domain.Constants;
using TodoSystem.Domain.Entities;
using TodoSystem.EntityFrameworkCore;
using TodoSystem.Infrastructure.Exceptions;
using TodoSystem.Infrastructure.Google;
using TodoSystem.Infrastructure.Token;

namespace TodoSystem.Application.Services.Auth;

/// <summary>
/// 认证业务。
///
/// 「解析用户」与「签发令牌」在方法层面分开：Authenticate / CreateLocalUser /
/// ResolveGoogleUser 只解析用户（Cookie 方案用），LoginToken / Register / LoginWithGoogle
/// 解析之后再签发令牌（JWT 方案用）。这样 Cookie 方案不会往 refresh_tokens 表里
/// 塞一堆没人用的孤儿令牌。
/// </summary>
public class AuthService(
    TodoSystemDbContext db,
    ITokenService tokenService,
    IPasswordHasher<UserEntity> passwordHasher,
    IGoogleTokenValidator googleTokenValidator,
    IEmailVerificationService emailVerification,
    ILogger<AuthService> logger) : IAuthService
{
    private const int MinPasswordLength = 6;

    /// <summary>用户名最大长度，与 ix_users_name 的 HasMaxLength 保持一致。</summary>
    private const int MaxNameLength = 50;

    /// <summary>验证用户身份：查用户、验密码、验账号状态。不产生 JWT。</summary>
    public async Task<UserAuthInfo> AuthenticateAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Name == request.UserName || u.Email == request.UserName, ct);

        // 统一提示，避免暴露账号是否存在
        if (user is null)
        {
            throw new CustomException(401, "用户名或密码错误");
        }

        // 纯第三方注册的账号没有本地密码，不能走密码登录
        if (!user.HasLocalPassword)
        {
            throw new CustomException(401, "该账号通过第三方登录注册，请使用 Google 登录");
        }

        var verifyResult = passwordHasher.VerifyHashedPassword(
            user,
            user.Password!,
            request.Password);

        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new CustomException(401, "用户名或密码错误");
        }

        if (!user.IsActivated)
        {
            throw new CustomException(403, "账号不可用");
        }

        return new UserAuthInfo(user.Id, user.Name, user.Email, user.Role);
    }

    public async Task<LoginResult> LoginTokenAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await AuthenticateAsync(request, ct);

        return await IssueTokensAsync(user, ct);
    }

    /// <summary>
    /// 本地账号注册，只创建用户、不签发令牌。
    /// 这里不验证邮箱归属，所以新账号的 EmailVerifiedAt 为 null。
    /// </summary>
    public async Task<UserAuthInfo> CreateLocalUserAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var name = request.UserName?.Trim() ?? string.Empty;
        var email = AuthValidation.NormalizeEmail(request.Email);

        ValidateRegistration(name, email, request.Password);

        var taken = await db.Users.AnyAsync(u => u.Name == name || u.Email == email, ct);

        if (taken)
        {
            throw new CustomException(409, "用户名或邮箱已被占用");
        }

        var user = new UserEntity
        {
            Name = name,
            Email = email,
            Role = UserRoles.User,
            IsActivated = true,
            // 邮箱未经验证：既不能作为账号合并的依据，也不允许被第三方账号自动接管
            EmailVerifiedAt = null,
            LastLoginAt = DateTimeOffset.UtcNow,
        };

        user.Password = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // 并发注册：上面的 AnyAsync 与这次 INSERT 之间被别的请求抢先了。
            // 唯一索引兜底，这里翻译成 409，避免泄漏成 500。
            db.Entry(user).State = EntityState.Detached;
            throw new CustomException(409, "用户名或邮箱已被占用");
        }

        await TrySendVerificationEmailAsync(user.Id, ct);

        return new UserAuthInfo(user.Id, user.Name, user.Email, user.Role);
    }

    /// <summary>本地账号注册 + 签发令牌。</summary>
    public async Task<LoginResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var user = await CreateLocalUserAsync(request, ct);

        return await IssueTokensAsync(user, ct);
    }

    /// <summary>
    /// Google 登录 / 首次登录即注册，只解析用户、不签发令牌。
    ///
    /// 匹配顺序：先按 (provider=google, sub) 找已有绑定（唯一可靠的身份依据）；
    /// 找不到就按邮箱合并到已验证邮箱的本地账号；邮箱未验证则拒绝合并并返回 409 ——
    /// 否则任何人抢先用别人的邮箱注册，就能在对方用 Google 登录时把对方的账号劫持进来。
    /// </summary>
    public async Task<UserAuthInfo> ResolveGoogleUserAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        var payload = await ValidateGooglePayloadAsync(request.IdToken, ct);

        var email = AuthValidation.NormalizeEmail(payload.Email);
        var now = DateTimeOffset.UtcNow;

        var binding = await db.ExternalLogins
            .FirstOrDefaultAsync(
                x => x.Provider == AuthProviders.Google && x.ProviderKey == payload.Subject,
                ct);

        UserEntity user;

        if (binding is not null)
        {
            user = await db.Users.FirstOrDefaultAsync(x => x.Id == binding.UserId, ct)
                   ?? throw new CustomException(401, "Google 账号绑定的用户已不存在");

            // 提供商侧信息可能变化，回填快照
            binding.Email = email;
            binding.DisplayName = payload.Name;
            binding.AvatarUrl = payload.Picture;
            binding.LastLoginAt = now;

            // 绑定关系存在本身就说明当初验证过邮箱，这里补登记一次
            user.EmailVerifiedAt ??= now;
        }
        else
        {
            user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, ct);

            if (user is not null && user.EmailVerifiedAt is null)
            {
                // 该邮箱被一个「从未验证过归属」的本地账号占着，不能自动合并
                throw new CustomException(
                    409,
                    "该邮箱已被注册。请先用邮箱密码登录，再到账号设置里绑定 Google。");
            }

            if (user is null)
            {
                user = new UserEntity
                {
                    Name = await GenerateUniqueNameAsync(payload.Name, email, ct),
                    Email = email,
                    // 第三方账号没有本地密码，只能走 Google 登录
                    Password = null,
                    Role = UserRoles.User,
                    IsActivated = true,
                    // Google 已经用 email_verified 证明了这个邮箱的归属
                    EmailVerifiedAt = now,
                    AvatarUrl = payload.Picture,
                };

                db.Users.Add(user);

                await db.SaveChangesAsync(ct);
            }

            db.ExternalLogins.Add(new ExternalLoginEntity
            {
                Provider = AuthProviders.Google,
                ProviderKey = payload.Subject,
                UserId = user.Id,
                Email = email,
                DisplayName = payload.Name,
                AvatarUrl = payload.Picture,
                LastLoginAt = now,
            });
        }

        if (!user.IsActivated)
        {
            throw new CustomException(403, "账号不可用");
        }

        user.LastLoginAt = now;

        if (string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            user.AvatarUrl = payload.Picture;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            // 同一个 sub 并发首次登录：另一个请求已经建好绑定
            throw new CustomException(409, "该 Google 账号刚刚已完成登录，请重试");
        }

        return new UserAuthInfo(user.Id, user.Name, user.Email, user.Role);
    }

    /// <summary>Google 登录 + 签发令牌。</summary>
    public async Task<LoginResult> LoginWithGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        var user = await ResolveGoogleUserAsync(request, ct);

        return await IssueTokensAsync(user, ct);
    }

    /// <summary>
    /// 把 Google 账号绑定到当前已登录用户。这是「本地账号后来想用 Google 登录」的唯一
    /// 安全通道：调用者必须已用密码证明自己是账号主人，才允许建立绑定。
    /// </summary>
    public async Task<UserProfile> LinkGoogleAsync(
        int userId,
        GoogleLoginRequest request,
        CancellationToken ct = default)
    {
        var payload = await ValidateGooglePayloadAsync(request.IdToken, ct);

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                   ?? throw new CustomException(404, "用户不存在");

        var existing = await db.ExternalLogins
            .FirstOrDefaultAsync(
                x => x.Provider == AuthProviders.Google && x.ProviderKey == payload.Subject,
                ct);

        if (existing is not null)
        {
            if (existing.UserId == userId)
            {
                // 幂等：重复绑定同一个 Google 账号直接返回
                return await GetProfileAsync(userId, ct);
            }

            throw new CustomException(409, "该 Google 账号已绑定到其他用户");
        }

        var alreadyBound = await db.ExternalLogins
            .AnyAsync(x => x.UserId == userId && x.Provider == AuthProviders.Google, ct);

        if (alreadyBound)
        {
            throw new CustomException(409, "当前账号已绑定其它 Google 账号，请先解绑");
        }

        var email = AuthValidation.NormalizeEmail(payload.Email);
        var now = DateTimeOffset.UtcNow;

        db.ExternalLogins.Add(new ExternalLoginEntity
        {
            Provider = AuthProviders.Google,
            ProviderKey = payload.Subject,
            UserId = user.Id,
            Email = email,
            DisplayName = payload.Name,
            AvatarUrl = payload.Picture,
            LastLoginAt = now,
        });

        // 同一个邮箱且 Google 已验证 → 本地邮箱也算验证过了
        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.EmailVerifiedAt ??= now;
        }

        if (string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            user.AvatarUrl = payload.Picture;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
        {
            throw new CustomException(409, "该 Google 账号已绑定到其他用户");
        }

        return await GetProfileAsync(userId, ct);
    }

    /// <summary>查询当前用户资料。</summary>
    public async Task<UserProfile> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                   ?? throw new CustomException(404, "用户不存在");

        var hasGoogle = await db.ExternalLogins
            .AnyAsync(x => x.UserId == userId && x.Provider == AuthProviders.Google, ct);

        return new UserProfile(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            user.AvatarUrl,
            user.HasLocalPassword,
            hasGoogle,
            user.IsEmailVerified);
    }

    /// <summary>
    /// 轮换宽限窗口。多个标签页可能同时拿同一个 Refresh Token 去刷新，
    /// 后到的那次会看到「刚刚被轮换」—— 那不是攻击，窗口内只拒绝本次请求、不吊销全链。
    /// 无论走哪条分支，本次请求都是 401，宽限窗口绝不会因此签发令牌。
    /// </summary>
    private static readonly TimeSpan RotationGraceWindow = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Refresh Token 换新的 Access + Refresh Token（轮换）。
    /// 已轮换的令牌被再次使用说明很可能已泄露，此时吊销该用户全部 Refresh Token；
    /// 宽限窗口内的并发刷新属于良性竞争，只拒绝本次请求。
    /// </summary>
    public async Task<LoginResult> RefreshAsync(
        RefreshRequest request,
        CancellationToken ct = default)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);

        var storedToken = await db.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

        if (storedToken is null)
        {
            throw new CustomException(401, "刷新令牌无效或已过期");
        }

        if (storedToken.RevokedAt is not null)
        {
            await RejectRevokedTokenAsync(storedToken, ct);
        }

        if (storedToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new CustomException(401, "刷新令牌无效或已过期");
        }

        var user = await db.Users
            .FirstOrDefaultAsync(x => x.Id == storedToken.UserId, ct);

        if (user is null || !user.IsActivated)
        {
            throw new CustomException(401, "用户不可用");
        }

        // 原子领取：并发刷新同一个令牌时，只有一个请求能把 revoked_at 从 NULL 改成有值，
        // 其余请求拿到 0 行受影响 —— 正好被识别为重放。
        DateTimeOffset? claimedAt = DateTimeOffset.UtcNow;

        var claimed = await db.RefreshTokens
            .Where(x => x.Id == storedToken.Id && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, claimedAt)
                    .SetProperty(x => x.RevokedReason, (string?)RefreshTokenRevocationReasons.Rotated),
                ct);

        if (claimed == 0)
        {
            // 另一个并发请求刚把令牌领走了。重新读一次最新状态再判定，
            // 避免把「刚被轮换」误判成攻击而吊销掉对方刚拿到的令牌。
            var latest = await db.RefreshTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == storedToken.Id, ct);

            await RejectRevokedTokenAsync(latest ?? storedToken, ct);
        }

        var result = await IssueTokensAsync(
            new UserAuthInfo(user.Id, user.Name, user.Email, user.Role),
            ct);

        // 记录轮换链，便于审计追溯
        await db.RefreshTokens
            .Where(x => x.Id == storedToken.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    x => x.ReplacedByTokenHash,
                    (string?)tokenService.HashToken(result.RefreshToken)),
                ct);

        return result;
    }

    /// <summary>注销并吊销 Refresh Token。</summary>
    public async Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct = default)
    {
        var query = db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null);

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = tokenService.HashToken(refreshToken);

            query = query.Where(x => x.TokenHash == tokenHash);
        }

        DateTimeOffset? revokedAt = DateTimeOffset.UtcNow;

        await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(x => x.RevokedAt, revokedAt)
                .SetProperty(x => x.RevokedReason, (string?)RefreshTokenRevocationReasons.Logout),
            ct);
    }

    /// <summary>创建 Access Token 和 Refresh Token。只有 JWT 方案会走到这里。</summary>
    private async Task<LoginResult> IssueTokensAsync(
        UserAuthInfo user,
        CancellationToken ct)
    {
        var accessToken = tokenService.CreateAccessToken(
            user.UserId,
            user.UserName,
            user.Email,
            user.Role);

        var refreshToken = tokenService.CreateRefreshToken();

        var refreshTokenEntity = new RefreshTokenEntity
        {
            UserId = user.UserId,
            TokenHash = tokenService.HashToken(refreshToken.Token),
            ExpiresAt = refreshToken.ExpiresAt,
        };

        // CreatedAt / UpdatedAt 由 TodoSystemDbContext 的审计逻辑统一写入
        db.RefreshTokens.Add(refreshTokenEntity);

        await db.SaveChangesAsync(ct);

        return new LoginResult(
            user.UserId,
            user.UserName,
            user.Role,
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresIn,
            refreshToken.ExpiresAt);
    }

    /// <summary>
    /// 处理「拿到一个已吊销的 Refresh Token」。
    /// 只有「轮换后的令牌被再次使用」才说明泄露，其它吊销原因（登出、已判定重放）
    /// 本身就已失效，再吊销全链没有额外意义。两种情况都抛 401。
    /// </summary>
    private async Task RejectRevokedTokenAsync(
        RefreshTokenEntity token,
        CancellationToken ct)
    {
        var wasRotated = token.RevokedReason == RefreshTokenRevocationReasons.Rotated;

        var withinGrace = token.RevokedAt is { } revokedAt
                          && DateTimeOffset.UtcNow - revokedAt <= RotationGraceWindow;

        if (wasRotated && !withinGrace)
        {
            await HandleReplayAsync(token.UserId, token.RevokedReason, ct);
        }

        if (wasRotated)
        {
            logger.LogInformation(
                "Refresh Token 并发刷新（宽限窗口内），用户 {UserId}，仅拒绝本次请求",
                token.UserId);
        }

        throw new CustomException(401, "刷新令牌无效或已过期");
    }

    /// <summary>检测到令牌重放：吊销该用户全部有效令牌并抛 401。</summary>
    private async Task HandleReplayAsync(
        int userId,
        string? originalReason,
        CancellationToken ct)
    {
        logger.LogWarning(
            "检测到 Refresh Token 重放，已吊销用户 {UserId} 的全部令牌。原吊销原因：{Reason}",
            userId,
            originalReason ?? "(unknown)");

        DateTimeOffset? revokedAt = DateTimeOffset.UtcNow;

        await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, revokedAt)
                    .SetProperty(x => x.RevokedReason, (string?)RefreshTokenRevocationReasons.ReplayDetected),
                ct);

        throw new CustomException(401, "检测到令牌异常使用，已强制下线，请重新登录");
    }

    /// <summary>
    /// 校验 Google id_token 并返回可信载荷。验签、验 aud、验有效期都由 validator 负责，
    /// 这里只做业务前提检查。
    /// </summary>
    private async Task<GoogleUserPayload> ValidateGooglePayloadAsync(
        string idToken,
        CancellationToken ct)
    {
        var payload = await googleTokenValidator.ValidateAsync(idToken, ct);

        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new CustomException(400, "Google 账号未返回邮箱，无法完成登录");
        }

        if (!payload.EmailVerified)
        {
            throw new CustomException(403, "Google 邮箱尚未验证，无法登录");
        }

        return payload;
    }

    private static void ValidateRegistration(string name, string email, string? password)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CustomException(400, "用户名不能为空");
        }

        if (name.Length > MaxNameLength)
        {
            throw new CustomException(400, $"用户名不能超过 {MaxNameLength} 个字符");
        }

        // 用户名和邮箱共用登录入口（AuthenticateAsync 里两者都能当用户名传），
        // 允许用户名带 @ 会让两者互相占用，这里直接禁止
        if (name.Contains('@'))
        {
            throw new CustomException(400, "用户名不能包含 @");
        }

        var parts = email.Split('@');

        if (string.IsNullOrWhiteSpace(email)
            || parts.Length != 2
            || parts[0].Length == 0
            || parts[1].Length == 0
            || email.Length > 200)
        {
            throw new CustomException(400, "邮箱格式不正确");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
        {
            throw new CustomException(400, $"密码至少 {MinPasswordLength} 位");
        }
    }

    /// <summary>
    /// 注册后发验证邮件。发信失败不阻断注册 —— 账号已经建好了，
    /// 用户还能从页面上的提示重新发送，为了邮件把整个注册判失败不划算。
    /// </summary>
    private async Task TrySendVerificationEmailAsync(int userId, CancellationToken ct)
    {
        try
        {
            await emailVerification.SendVerificationEmailAsync(userId, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "验证邮件发送失败，用户 {UserId} 需要自行重发", userId);
        }
    }

    /// <summary>Google 昵称可能为空、可能重名，这里生成一个可用的唯一用户名。</summary>
    private async Task<string> GenerateUniqueNameAsync(
        string? preferred,
        string email,
        CancellationToken ct)
    {
        var baseName = !string.IsNullOrWhiteSpace(preferred)
            ? preferred.Trim()
            : email.Split('@')[0];

        // 去掉 @，否则会和邮箱登录入口互相占用
        baseName = baseName.Replace("@", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "user";
        }

        if (baseName.Length > MaxNameLength - 10)
        {
            baseName = baseName[..(MaxNameLength - 10)];
        }

        var candidate = baseName;
        var suffix = 1;

        while (await db.Users.AnyAsync(u => u.Name == candidate, ct))
        {
            candidate = $"{baseName}_{suffix++}";

            if (suffix > 1000)
            {
                // 兜底：用 GUID 保证不撞，同时裁到字段长度以内
                var guid = Guid.NewGuid().ToString("N");
                var keep = Math.Max(1, MaxNameLength - guid.Length - 1);
                candidate = $"{baseName[..Math.Min(baseName.Length, keep)]}_{guid}";
                break;
            }
        }

        return candidate;
    }
}
