using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class RoomRepository(AppDbContext db) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Rooms
          .Include(r => r.Streamer)
          .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<Room>> GetLiveRoomsAsync(CancellationToken ct = default) =>
        db.Rooms
          .Where(r => r.Status == RoomStatus.Live)
          .Include(r => r.Streamer)
          .OrderByDescending(r => r.StartedAt)
          .ToListAsync(ct);

    async Task<IReadOnlyList<Room>> IRoomRepository.GetLiveRoomsAsync(CancellationToken ct) =>
        await GetLiveRoomsAsync(ct);

    public async Task<IReadOnlyList<Room>> GetByStreamerIdAsync(Guid streamerId, CancellationToken ct = default) =>
        await db.Rooms
                .Where(r => r.StreamerId == streamerId)
                .Include(r => r.Streamer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);

    public async Task<Room> AddAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Add(room);
        await db.SaveChangesAsync(ct);
        return room;
    }

    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Update(room);
        await db.SaveChangesAsync(ct);
    }

    public Task<int> GetParticipantCountAsync(Guid roomId, CancellationToken ct = default) =>
        db.RoomParticipants.CountAsync(p => p.RoomId == roomId, ct);

    public async Task AddParticipantAsync(RoomParticipant participant, CancellationToken ct = default)
    {
        db.RoomParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        await db.RoomParticipants
                .Where(p => p.RoomId == roomId && p.UserId == userId)
                .ExecuteDeleteAsync(ct);
    }

    public Task<bool> IsParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default) =>
        db.RoomParticipants.AnyAsync(p => p.RoomId == roomId && p.UserId == userId, ct);
}
