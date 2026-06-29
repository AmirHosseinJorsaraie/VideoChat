using ChatApp.Core.Entities;
using ChatApp.Core.Enums;

namespace ChatApp.Core.Interfaces.Repositories;

public interface IVideoCallRepository
{
    Task<VideoCall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VideoCall?> GetActiveCallForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<VideoCall>> GetCallHistoryAsync(Guid userId, int skip = 0, int take = 20, CancellationToken ct = default);
    Task<VideoCall> AddAsync(VideoCall call, CancellationToken ct = default);
    Task UpdateAsync(VideoCall call, CancellationToken ct = default);
}
