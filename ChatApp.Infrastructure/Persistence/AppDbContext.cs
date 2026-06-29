using ChatApp.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RoomParticipant> RoomParticipants => Set<RoomParticipant>();
    public DbSet<VideoCall> VideoCalls => Set<VideoCall>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── AppUser ───────────────────────────────────────────────────────────
        builder.Entity<AppUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role).HasConversion<string>();
        });

        // ── Room ─────────────────────────────────────────────────────────────
        builder.Entity<Room>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(200).IsRequired();
            e.Property(r => r.Description).HasMaxLength(1000);
            e.Property(r => r.Status).HasConversion<string>();

            e.HasOne(r => r.Streamer)
             .WithMany(u => u.OwnedRooms)
             .HasForeignKey(r => r.StreamerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Message ───────────────────────────────────────────────────────────
        builder.Entity<Message>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Content).HasMaxLength(2000).IsRequired();

            e.HasOne(m => m.Room)
             .WithMany(r => r.Messages)
             .HasForeignKey(m => m.RoomId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Sender)
             .WithMany(u => u.Messages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            // Filter soft-deleted messages by default
            e.HasQueryFilter(m => !m.IsDeleted);
        });

        // ── RoomParticipant ───────────────────────────────────────────────────
        builder.Entity<RoomParticipant>(e =>
        {
            e.HasKey(rp => new { rp.RoomId, rp.UserId });

            e.HasOne(rp => rp.Room)
             .WithMany(r => r.Participants)
             .HasForeignKey(rp => rp.RoomId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rp => rp.User)
             .WithMany(u => u.RoomParticipations)
             .HasForeignKey(rp => rp.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── VideoCall ─────────────────────────────────────────────────────────
        builder.Entity<VideoCall>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Status).HasConversion<string>();

            e.HasOne(c => c.Caller)
             .WithMany(u => u.InitiatedCalls)
             .HasForeignKey(c => c.CallerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Callee)
             .WithMany(u => u.ReceivedCalls)
             .HasForeignKey(c => c.CalleeId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.Room)
             .WithMany()
             .HasForeignKey(c => c.RoomId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
