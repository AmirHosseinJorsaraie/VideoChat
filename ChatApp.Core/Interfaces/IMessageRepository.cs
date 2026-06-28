using ChatApp.Core.Entities;

namespace ChatApp.Core.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Message>> GetByRoomIdAsync(Guid roomId, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<Message> AddAsync(Message message, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid messageId, CancellationToken ct = default);
}
