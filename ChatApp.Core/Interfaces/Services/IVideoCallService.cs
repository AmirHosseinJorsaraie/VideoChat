using ChatApp.Core.DTOs;

namespace ChatApp.Core.Interfaces.Services;

/// <summary>
/// Abstracts the video call lifecycle. 
/// 
/// V1 implementation: WebRtcPeerService (pure P2P signaling via SignalR)
/// V2 implementation: LiveKitService   (SFU-based multi-party)
/// 
/// Swap implementations by changing one DI registration in Program.cs.
/// The Blazor UI and VideoHub only depend on this interface.
/// </summary>
public interface IVideoCallService
{
    /// <summary>Creates a pending call record and notifies the callee.</summary>
    Task<VideoCallDto> InitiateCallAsync(InitiateCallRequest request, Guid callerId, CancellationToken ct = default);

    /// <summary>Callee accepts — marks call Active, starts WebRTC handshake.</summary>
    Task<VideoCallDto> AcceptCallAsync(Guid callId, Guid calleeId, CancellationToken ct = default);

    /// <summary>Either party ends the call.</summary>
    Task EndCallAsync(Guid callId, Guid requestingUserId, CancellationToken ct = default);

    /// <summary>Callee explicitly rejects the incoming call.</summary>
    Task RejectCallAsync(Guid callId, Guid calleeId, CancellationToken ct = default);

    /// <summary>Gets the active call for a user, if any.</summary>
    Task<VideoCallDto?> GetActiveCallAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<VideoCallDto>> GetCallHistoryAsync(Guid userId, int skip = 0, int take = 20, CancellationToken ct = default);
}
