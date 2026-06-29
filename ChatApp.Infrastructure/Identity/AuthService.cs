using ChatApp.Core.DTOs;
using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ChatApp.Application.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            UserName = request.Username.ToLower().Trim(),
            Email = request.Email.ToLower().Trim(),
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return new AuthResult(null!, false, result.Errors.Select(e => e.Description));

        // Assign the Identity role matching the enum value
        await userManager.AddToRoleAsync(user, request.Role.ToString());

        // Sign in immediately after registration
        await signInManager.SignInAsync(user, isPersistent: true);

        return new AuthResult(ToDto(user), true, []);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.ToLower().Trim());
        if (user is null)
            return new AuthResult(null!, false, ["Invalid email or password."]);

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
            return new AuthResult(null!, false, ["Invalid email or password."]);

        return new AuthResult(ToDto(user), true, []);
    }

    public async Task LogoutAsync() =>
        await signInManager.SignOutAsync();

    public async Task<UserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var httpUser = httpContextAccessor.HttpContext?.User;
        if (httpUser is null || !httpUser.Identity?.IsAuthenticated == true)
            return null;

        var user = await userManager.GetUserAsync(httpUser);
        return user is null ? null : ToDto(user);
    }

    private static UserDto ToDto(AppUser user) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.DisplayName,
        user.Email ?? string.Empty,
        user.Role,
        user.IsOnline
    );
}
