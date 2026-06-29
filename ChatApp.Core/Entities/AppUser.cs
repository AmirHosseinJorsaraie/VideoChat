using ChatApp.Core.Enums;
using Microsoft.AspNetCore.Identity;
namespace ChatApp.Core.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOnline { get; set; } = false;

    // Navigation
    public ICollection<Room> OwnedRooms { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<RoomParticipant> RoomParticipations { get; set; } = [];

    // Video calls — calls initiated by this user
    public ICollection<VideoCall> InitiatedCalls { get; set; } = [];

    // Video calls — calls received by this user
    public ICollection<VideoCall> ReceivedCalls { get; set; } = [];

    public bool IsStreamer => Role == UserRole.Streamer;
}
