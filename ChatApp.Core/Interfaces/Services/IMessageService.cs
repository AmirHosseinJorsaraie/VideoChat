using ChatApp.Core.DTOs;

namespace ChatApp.Core.Interfaces.Services;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(SendMessageRequest request, Guid senderId, CancellationToken ct = default);
    Task<IReadOnlyList<MessageDto>> GetRoomHistoryAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task DeleteMessageAsync(Guid messageId, Guid requestingUserId, CancellationToken ct = default);
}
