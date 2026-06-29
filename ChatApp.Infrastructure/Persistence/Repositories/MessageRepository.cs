using ChatApp.Core.Entities;
using ChatApp.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class MessageRepository(AppDbContext db) : IMessageRepository
{
    public Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Messages
          .Include(m => m.Sender)
          .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<Message>> GetByRoomIdAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        await db.Messages
                .Where(m => m.RoomId == roomId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

    public async Task<Message> AddAsync(Message message, CancellationToken ct = default)
    {
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task SoftDeleteAsync(Guid messageId, CancellationToken ct = default)
    {
        await db.Messages
                .Where(m => m.Id == messageId)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsDeleted, true), ct);
    }
}
