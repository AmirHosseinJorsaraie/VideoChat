namespace ChatApp.Core.Entities;

/// <summary>
/// Tracks who is currently watching a live room.
/// Used for live viewer counts and presence.
/// </summary>
public class RoomParticipant
{
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Room Room { get; set; } = null!;
    public AppUser User { get; set; } = null!;
}
