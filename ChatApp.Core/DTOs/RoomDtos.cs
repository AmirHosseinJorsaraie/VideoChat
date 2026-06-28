using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

public record RoomDto(
    Guid Id,
    string Title,
    string? Description,
    RoomStatus Status,
    Guid StreamerId,
    string StreamerUsername,
    int ViewerCount,
    DateTime CreatedAt,
    DateTime? StartedAt
);

public record CreateRoomRequest(
    string Title,
    string? Description
);
