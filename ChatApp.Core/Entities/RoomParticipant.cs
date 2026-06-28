namespace ChatApp.Core.Entities;

/// <summary>
/// Tracks active participants in a room (joined but not yet left).
/// Used to show viewer counts and manage SignalR group membership.
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
