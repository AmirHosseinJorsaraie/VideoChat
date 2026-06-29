using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

public record MessageDto(
    Guid Id,
    string Content,
    Guid SenderId,
    string SenderUsername,
    string SenderDisplayName,
    bool IsSenderStreamer,
    DateTime SentAt
);

public record SendMessageRequest(
    Guid RoomId,
    string Content
);
