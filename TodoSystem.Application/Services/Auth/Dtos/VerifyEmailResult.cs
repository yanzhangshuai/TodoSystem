namespace TodoSystem.Application.Services.Auth.Dtos;

/// <summary>邮箱验证成功的结果。带上邮箱，前端才能明确告诉用户验证的是哪一个。</summary>
public record VerifyEmailResult(string Email);
