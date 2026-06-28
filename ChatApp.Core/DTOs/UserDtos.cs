using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role
);

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    UserRole Role
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResult(
    UserDto User,
    string Token
);
