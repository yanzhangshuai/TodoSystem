namespace TodoSystem.Web.Controllers.Requests;

/// <summary>本地账号注册入参。</summary>
public class RegisterInput
{
    public string UserName { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;
}

/// <summary>用户名（或邮箱）+ 密码登录入参。</summary>
public class LoginInput
{
    public string UserName { get; set; } = default!;

    public string Password { get; set; } = default!;
}

/// <summary>登出入参。</summary>
public class LogoutInput
{
    /// <summary>可选。只吊销指定 Refresh Token；为空则吊销当前用户全部。</summary>
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Google Identity Services 返回的 id_token 入参。登录和绑定共用同一个模型。
/// </summary>
public class GoogleIdTokenInput
{
    public string IdToken { get; set; } = default!;
}
