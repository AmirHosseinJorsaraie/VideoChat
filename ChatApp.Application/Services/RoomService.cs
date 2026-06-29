using ChatApp.Core.DTOs;
using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Exceptions;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;

namespace ChatApp.Application.Services;

public class RoomService(IRoomRepository roomRepo, IUserRepository userRepo) : IRoomService
{
    public async Task<RoomDto> CreateRoomAsync(CreateRoomRequest request, Guid streamerId, CancellationToken ct = default)
    {
        var streamer = await userRepo.GetByIdAsync(streamerId, ct)
            ?? throw new NotFoundException(nameof(AppUser), streamerId);

        if (!streamer.IsStreamer)
            throw new UnauthorizedException("Only streamers can create rooms.");

        var room = new Room
        {
            Title = request.Title,
            Description = request.Description,
            StreamerId = streamerId,
            Status = RoomStatus.Offline
        };

        var created = await roomRepo.AddAsync(room, ct);
        return ToDto(created, streamer, 0);
    }

    public async Task<RoomDto> StartStreamAsync(Guid roomId, Guid streamerId, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);

        if (room.StreamerId != streamerId)
            throw new UnauthorizedException("Only the room owner can start the stream.");

        if (room.IsLive)
            throw new RoomAlreadyLiveException(roomId);

        room.Status = RoomStatus.Live;
        room.StartedAt = DateTime.UtcNow;

        await roomRepo.UpdateAsync(room, ct);

        var streamer = await userRepo.GetByIdAsync(streamerId, ct)!;
        return ToDto(room, streamer!, 0);
    }

    public async Task EndStreamAsync(Guid roomId, Guid streamerId, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);

        if (room.StreamerId != streamerId)
            throw new UnauthorizedException("Only the room owner can end the stream.");

        if (!room.IsLive)
            throw new RoomNotLiveException(roomId);

        room.Status = RoomStatus.Ended;
        room.EndedAt = DateTime.UtcNow;

        await roomRepo.UpdateAsync(room, ct);
    }

    public async Task<RoomDto?> GetRoomAsync(Guid roomId, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(roomId, ct);
        if (room is null) return null;

        var count = await roomRepo.GetParticipantCountAsync(roomId, ct);
        return ToDto(room, room.Streamer, count);
    }

    public async Task<IReadOnlyList<RoomDto>> GetLiveRoomsAsync(CancellationToken ct = default)
    {
        var rooms = await roomRepo.GetLiveRoomsAsync(ct);
        var result = new List<RoomDto>();

        foreach (var room in rooms)
        {
            var count = await roomRepo.GetParticipantCountAsync(room.Id, ct);
            result.Add(ToDto(room, room.Streamer, count));
        }

        return result;
    }

    public async Task<IReadOnlyList<RoomDto>> GetStreamerHistoryAsync(Guid streamerId, CancellationToken ct = default)
    {
        var rooms = await roomRepo.GetByStreamerIdAsync(streamerId, ct);
        var streamer = await userRepo.GetByIdAsync(streamerId, ct);

        return rooms.Select(r => ToDto(r, streamer!, 0)).ToList();
    }

    public async Task JoinRoomAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(roomId, ct)
            ?? throw new NotFoundException(nameof(Room), roomId);

        if (!room.IsLive)
            throw new RoomNotLiveException(roomId);

        var alreadyIn = await roomRepo.IsParticipantAsync(roomId, userId, ct);
        if (alreadyIn) return;

        await roomRepo.AddParticipantAsync(new RoomParticipant
        {
            RoomId = roomId,
            UserId = userId
        }, ct);
    }

    public async Task LeaveRoomAsync(Guid roomId, Guid userId, CancellationToken ct = default)
    {
        await roomRepo.RemoveParticipantAsync(roomId, userId, ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static RoomDto ToDto(Room room, AppUser streamer, int viewerCount) => new(
        room.Id,
        room.Title,
        room.Description,
        room.Status,
        room.StreamerId,
        streamer.UserName ?? string.Empty,
        streamer.DisplayName,
        viewerCount,
        room.CreatedAt,
        room.StartedAt,
        room.EndedAt
    );
}
