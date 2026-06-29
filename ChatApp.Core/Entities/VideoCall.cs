using ChatApp.Core.Enums;

namespace ChatApp.Core.Entities;

/// <summary>
/// Represents a WebRTC video call session between two users (V1).
/// 
/// V2 migration path: Add a VideoCallParticipant collection and
/// replace the CallerId/CalleeId pattern with a participants table.
/// IVideoCallService abstraction means the Blazor UI won't change.
/// </summary>
public class VideoCall
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CallStatus Status { get; set; } = CallStatus.Pending;
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }

    // The room this call originated from (optional — calls can be initiated
    // from a room's chat or directly from a user profile)
    public Guid? RoomId { get; set; }

    // V1: direct caller ↔ callee
    public Guid CallerId { get; set; }
    public Guid CalleeId { get; set; }

    // Navigation
    public AppUser Caller { get; set; } = null!;
    public AppUser Callee { get; set; } = null!;
    public Room? Room { get; set; }

    public TimeSpan? Duration => AnsweredAt.HasValue
        ? (EndedAt ?? DateTime.UtcNow) - AnsweredAt.Value
        : null;
}
