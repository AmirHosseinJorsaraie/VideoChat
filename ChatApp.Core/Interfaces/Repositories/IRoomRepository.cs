using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetLiveRoomsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Room>> GetByStreamerIdAsync(Guid streamerId, CancellationToken ct = default);
    Task<Room> AddAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
    Task<int> GetParticipantCountAsync(Guid roomId, CancellationToken ct = default);
    Task AddParticipantAsync(RoomParticipant participant, CancellationToken ct = default);
    Task RemoveParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default);
    Task<bool> IsParticipantAsync(Guid roomId, Guid userId, CancellationToken ct = default);
}
