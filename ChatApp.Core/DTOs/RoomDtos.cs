using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

public record RoomDto(
    Guid Id,
    string Title,
    string? Description,
    RoomStatus Status,
    Guid StreamerId,
    string StreamerUsername,
    string StreamerDisplayName,
    int ViewerCount,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? EndedAt
)
{
    public bool IsLive => Status == RoomStatus.Live;
};


public record CreateRoomRequest(
    string Title,
    string? Description
);
