using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task UpdateAsync(AppUser user, CancellationToken ct = default);
    Task SetOnlineStatusAsync(Guid userId, bool isOnline, CancellationToken ct = default);
}
