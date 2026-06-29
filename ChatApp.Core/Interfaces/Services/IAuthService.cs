using ChatApp.Core.DTOs;

namespace ChatApp.Core.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default);
}
