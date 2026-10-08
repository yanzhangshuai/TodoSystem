namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>
/// 前端启动时需要的公开配置。
/// Google ClientId 本身就是公开值（会出现在前端请求里），可以安全下发。
/// </summary>
public record AuthConfig(string GoogleClientId);
