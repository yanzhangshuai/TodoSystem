namespace TodoSystem.Application.Services.Auth.Dtos;

public record UserAuthInfo(
    int UserId,
    string UserName,
    string Email,
    string Role);