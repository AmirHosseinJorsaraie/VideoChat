namespace ChatApp.Core.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Foreign keys
    public Guid RoomId { get; set; }
    public Guid SenderId { get; set; }

    // Navigation
    public Room Room { get; set; } = null!;
    public AppUser Sender { get; set; } = null!;
}
