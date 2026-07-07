using ChatApp.Core.DTOs;
using ChatApp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Infrastructure.SignalR;

/// <summary>
/// WebRTC signaling relay for 1-to-1 video calls (V1).
///
/// This hub does NOT process video/audio — it only relays SDP offers/answers
/// and ICE candidates between the two peers so they can establish a direct
/// P2P connection. Once connected, media flows directly browser-to-browser.
///
/// V2 migration: replace relay logic here with LiveKit token issuance.
/// The client-facing method names (ReceiveOffer, ReceiveAnswer, etc.) can
/// stay the same so Blazor components need minimal changes.
///
/// Client methods called by this hub:
///   - IncomingCall(VideoCallDto)
///   - CallAccepted(Guid callId)
///   - CallRejected(Guid callId)
///   - CallEnded(Guid callId)
///   - ReceiveOffer(SdpSignalDto)
///   - ReceiveAnswer(SdpSignalDto)
///   - ReceiveIceCandidate(IceCandidateDto)
/// </summary>
[Authorize]
public class VideoHub(IVideoCallService callService) : Hub
{
    // Maps userId → connectionId for routing signaling messages
    private static readonly Dictionary<Guid, string> _connections = new();
    private static readonly object _lock = new();

    // ── Connection lifecycle ──────────────────────────────────────────────────

    public override Task OnConnectedAsync()
    {
        var userId = GetUserId();
        lock (_lock) _connections[userId] = Context.ConnectionId;
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        // End any active call on disconnect
        var activeCall = await callService.GetActiveCallAsync(userId);
        if (activeCall is not null)
        {
            await callService.EndCallAsync(activeCall.Id, userId);
            await NotifyOtherParty(activeCall, userId, "CallEnded", activeCall.Id);
        }

        lock (_lock) _connections.Remove(userId);
        await base.OnDisconnectedAsync(exception);
    }

    // ── Call lifecycle ────────────────────────────────────────────────────────

    public async Task<VideoCallDto> InitiateCall(Guid calleeId, Guid? roomId)
    {
        var callerId = GetUserId();
        var request = new InitiateCallRequest(calleeId, roomId);
        var call = await callService.InitiateCallAsync(request, callerId);

        // Notify callee if they're connected
        var calleeConnectionId = GetConnectionId(calleeId);
        if (calleeConnectionId is not null)
            await Clients.Client(calleeConnectionId).SendAsync("IncomingCall", call);

        return call;
    }

    public async Task<VideoCallDto> AcceptCall(Guid callId)
    {
        var calleeId = GetUserId();
        var call = await callService.AcceptCallAsync(callId, calleeId);

        // Tell the caller their call was accepted — they should now send the SDP offer
        var callerConnectionId = GetConnectionId(call.CallerId);
        if (callerConnectionId is not null)
            await Clients.Client(callerConnectionId).SendAsync("CallAccepted", callId);

        return call;
    }

    public async Task RejectCall(Guid callId)
    {
        var calleeId = GetUserId();
        var call = await callService.GetActiveCallAsync(calleeId);
        if (call is null) return;

        await callService.RejectCallAsync(callId, calleeId);

        var callerConnectionId = GetConnectionId(call.CallerId);
        if (callerConnectionId is not null)
            await Clients.Client(callerConnectionId).SendAsync("CallRejected", callId);
    }

    public async Task EndCall(Guid callId)
    {
        var userId = GetUserId();
        var call = await callService.GetActiveCallAsync(userId);
        if (call is null) return;

        await callService.EndCallAsync(callId, userId);
        await NotifyOtherParty(call, userId, "CallEnded", callId);
    }

    // ── WebRTC signaling relay ────────────────────────────────────────────────
    // The hub just routes these to the other party — it never inspects the payload.

    public async Task SendOffer(SdpSignalDto signal)
    {
        var userId = GetUserId();
        var call = await callService.GetActiveCallAsync(userId)
            ?? throw new HubException("No active call found.");

        var otherConnectionId = GetOtherPartyConnectionId(call, userId);
        if (otherConnectionId is not null)
            await Clients.Client(otherConnectionId).SendAsync("ReceiveOffer", signal);
    }

    public async Task SendAnswer(SdpSignalDto signal)
    {
        var userId = GetUserId();
        var call = await callService.GetActiveCallAsync(userId)
            ?? throw new HubException("No active call found.");

        var otherConnectionId = GetOtherPartyConnectionId(call, userId);
        if (otherConnectionId is not null)
            await Clients.Client(otherConnectionId).SendAsync("ReceiveAnswer", signal);
    }

    public async Task SendIceCandidate(IceCandidateDto candidate)
    {
        var userId = GetUserId();
        var call = await callService.GetActiveCallAsync(userId)
            ?? throw new HubException("No active call found.");

        var otherConnectionId = GetOtherPartyConnectionId(call, userId);
        if (otherConnectionId is not null)
            await Clients.Client(otherConnectionId).SendAsync("ReceiveIceCandidate", candidate);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(claim ?? throw new HubException("User not authenticated."));
    }

    private static string? GetConnectionId(Guid userId)
    {
        lock (_lock) return _connections.TryGetValue(userId, out var id) ? id : null;
    }

    private static string? GetOtherPartyConnectionId(VideoCallDto call, Guid currentUserId)
    {
        var otherId = call.CallerId == currentUserId ? call.CalleeId : call.CallerId;
        return GetConnectionId(otherId);
    }

    private async Task NotifyOtherParty(VideoCallDto call, Guid currentUserId, string method, object payload)
    {
        var connectionId = GetOtherPartyConnectionId(call, currentUserId);
        if (connectionId is not null)
            await Clients.Client(connectionId).SendAsync(method, payload);
    }
}
