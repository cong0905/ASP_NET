namespace ShopNet.Application.Features.Auth.DTOs;

public record AuthResponseDto(
    string Token,
    string RefreshToken,
    DateTime ExpiresAt,
    int UserId,
    string Username,
    string Email,
    string FullName,
    string Role
);

public record UserDto(
    int Id,
    string Username,
    string Email,
    string FullName,
    string Role
);
