using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Infrastructure.SignalR;

/// <summary>
/// Handles real-time chat messaging and room presence.
/// Client methods (called via JS/Blazor interop):
///   - ReceiveMessage(MessageDto)
///   - ViewerCountUpdated(int)
///   - StreamEnded()
///   - StreamStarted()
/// </summary>
[Authorize]
public class ChatHub(IRoomService roomService, IMessageService messageService) : Hub
{
    // ── Connection lifecycle ──────────────────────────────────────────────────

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Room leave is handled by LeaveRoom; this handles unexpected disconnects
        await base.OnDisconnectedAsync(exception);
    }

    // ── Room management ───────────────────────────────────────────────────────

    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetUserId();

        await roomService.JoinRoomAsync(roomId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroup(roomId));

        var room = await roomService.GetRoomAsync(roomId);
        if (room is not null)
            await Clients.Group(RoomGroup(roomId)).SendAsync("ViewerCountUpdated", room.ViewerCount);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetUserId();

        await roomService.LeaveRoomAsync(roomId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroup(roomId));

        var room = await roomService.GetRoomAsync(roomId);
        if (room is not null)
            await Clients.Group(RoomGroup(roomId)).SendAsync("ViewerCountUpdated", room.ViewerCount);
    }

    // ── Chat messaging ────────────────────────────────────────────────────────

    public async Task SendMessage(Guid roomId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var userId = GetUserId();
        var request = new SendMessageRequest(roomId, content);
        var message = await messageService.SendMessageAsync(request, userId);

        // Broadcast to everyone in the room (including sender)
        await Clients.Group(RoomGroup(roomId)).SendAsync("ReceiveMessage", message);
    }

    public async Task DeleteMessage(Guid messageId, Guid roomId)
    {
        var userId = GetUserId();
        await messageService.DeleteMessageAsync(messageId, userId);

        await Clients.Group(RoomGroup(roomId)).SendAsync("MessageDeleted", messageId);
    }

    // ── Stream control (streamer only) ────────────────────────────────────────

    [Authorize(Roles = "Streamer")]
    public async Task NotifyStreamStarted(Guid roomId)
    {
        await Clients.Group(RoomGroup(roomId)).SendAsync("StreamStarted", roomId);
    }

    [Authorize(Roles = "Streamer")]
    public async Task NotifyStreamEnded(Guid roomId)
    {
        await Clients.Group(RoomGroup(roomId)).SendAsync("StreamEnded", roomId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim ?? throw new HubException("User not authenticated."));
    }

    private static string RoomGroup(Guid roomId) => $"room-{roomId}";
}
