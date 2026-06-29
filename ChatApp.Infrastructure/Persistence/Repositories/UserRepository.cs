using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FindAsync([id], ct).AsTask();

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.UserName == username.ToLower(), ct);

    public async Task UpdateAsync(AppUser user, CancellationToken ct = default)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetOnlineStatusAsync(Guid userId, bool isOnline, CancellationToken ct = default)
    {
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsOnline, isOnline), ct);
    }
}
