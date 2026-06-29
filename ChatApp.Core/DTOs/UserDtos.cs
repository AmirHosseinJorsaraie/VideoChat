using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

public record UserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    UserRole Role,
    bool IsOnline
);

public record RegisterRequest(
    string Username,
    string DisplayName,
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
    bool Succeeded,
    IEnumerable<string> Errors
);
