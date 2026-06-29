using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class VideoCallRepository(AppDbContext db) : IVideoCallRepository
{
    public Task<VideoCall?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.VideoCalls
          .Include(c => c.Caller)
          .Include(c => c.Callee)
          .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<VideoCall?> GetActiveCallForUserAsync(Guid userId, CancellationToken ct = default) =>
        db.VideoCalls
          .Where(c => (c.CallerId == userId || c.CalleeId == userId)
                   && (c.Status == CallStatus.Pending || c.Status == CallStatus.Active))
          .Include(c => c.Caller)
          .Include(c => c.Callee)
          .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<VideoCall>> GetCallHistoryAsync(Guid userId, int skip = 0, int take = 20, CancellationToken ct = default) =>
        await db.VideoCalls
                .Where(c => c.CallerId == userId || c.CalleeId == userId)
                .Include(c => c.Caller)
                .Include(c => c.Callee)
                .OrderByDescending(c => c.InitiatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

    public async Task<VideoCall> AddAsync(VideoCall call, CancellationToken ct = default)
    {
        db.VideoCalls.Add(call);
        await db.SaveChangesAsync(ct);
        return call;
    }

    public async Task UpdateAsync(VideoCall call, CancellationToken ct = default)
    {
        db.VideoCalls.Update(call);
        await db.SaveChangesAsync(ct);
    }
}
