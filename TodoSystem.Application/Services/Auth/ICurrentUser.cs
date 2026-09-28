namespace TodoSystem.Application.Services.Auth;

/// <summary>
/// 当前登录用户。屏蔽 Claims 细节，业务侧只读 Id / UserName / Role。
/// </summary>
public interface ICurrentUser
{
    int Id { get; }

    string? UserName { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
