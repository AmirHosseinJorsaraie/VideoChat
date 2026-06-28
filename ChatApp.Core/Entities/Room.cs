using ChatApp.Core.Enums;

namespace ChatApp.Core.Entities;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Offline;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // Foreign key
    public Guid StreamerId { get; set; }

    // Navigation
    public AppUser Streamer { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<RoomParticipant> Participants { get; set; } = [];

    public bool IsLive => Status == RoomStatus.Live;
}
