namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>
/// Google 登录入参。
/// IdToken 由前端 Google Identity Services 弹窗拿到，后端负责验签，不可直接信任其内容。
/// </summary>
public record GoogleLoginRequest(string IdToken);
