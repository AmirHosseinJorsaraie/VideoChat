using ChatApp.Core.DTOs;

namespace ChatApp.Core.Interfaces;

public interface IRoomService
{
    Task<RoomDto> CreateRoomAsync(CreateRoomRequest request, Guid streamerId, CancellationToken ct = default);
    Task<RoomDto> StartStreamAsync(Guid roomId, Guid streamerId, CancellationToken ct = default);
    Task EndStreamAsync(Guid roomId, Guid streamerId, CancellationToken ct = default);
    Task<RoomDto?> GetRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<IReadOnlyList<RoomDto>> GetLiveRoomsAsync(CancellationToken ct = default);
    Task JoinRoomAsync(Guid roomId, Guid userId, CancellationToken ct = default);
    Task LeaveRoomAsync(Guid roomId, Guid userId, CancellationToken ct = default);
}
