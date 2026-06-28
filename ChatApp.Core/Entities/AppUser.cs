using ChatApp.Core.Enums;

namespace ChatApp.Core.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Room> OwnedRooms { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];

    public bool IsStreamer => Role == UserRole.Streamer;
}
