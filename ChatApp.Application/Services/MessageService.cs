using ChatApp.Core.DTOs;
using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Exceptions;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;

namespace ChatApp.Application.Services;

public class MessageService(
    IMessageRepository messageRepo,
    IRoomRepository roomRepo,
    IUserRepository userRepo) : IMessageService
{
    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId, CancellationToken ct = default)
    {
        var room = await roomRepo.GetByIdAsync(request.RoomId, ct)
            ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!room.IsLive)
            throw new RoomNotLiveException(request.RoomId);

        var sender = await userRepo.GetByIdAsync(senderId, ct)
            ?? throw new NotFoundException(nameof(AppUser), senderId);

        var message = new Message
        {
            Content = request.Content.Trim(),
            RoomId = request.RoomId,
            SenderId = senderId
        };

        var saved = await messageRepo.AddAsync(message, ct);
        return ToDto(saved, sender);
    }

    public async Task<IReadOnlyList<MessageDto>> GetRoomHistoryAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken ct = default)
    {
        var messages = await messageRepo.GetByRoomIdAsync(roomId, skip, take, ct);
        return messages.Select(m => ToDto(m, m.Sender)).ToList();
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid requestingUserId, CancellationToken ct = default)
    {
        var message = await messageRepo.GetByIdAsync(messageId, ct)
            ?? throw new NotFoundException(nameof(Message), messageId);

        var room = await roomRepo.GetByIdAsync(message.RoomId, ct)!;

        // Only the message author or the room's streamer can delete
        bool isAuthor = message.SenderId == requestingUserId;
        bool isStreamer = room!.StreamerId == requestingUserId;

        if (!isAuthor && !isStreamer)
            throw new UnauthorizedException("Only the message author or room streamer can delete messages.");

        await messageRepo.SoftDeleteAsync(messageId, ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static MessageDto ToDto(Message message, AppUser sender) => new(
        message.Id,
        message.Content,
        message.SenderId,
        sender.UserName ?? string.Empty,
        sender.DisplayName,
        sender.Role == UserRole.Streamer,
        message.SentAt
    );
}
