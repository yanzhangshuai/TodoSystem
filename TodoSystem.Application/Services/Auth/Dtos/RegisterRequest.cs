namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>本地账号注册入参。</summary>
public record RegisterRequest(
    string UserName,
    string Email,
    string Password);
