using ChatApp.Core.DTOs;
using ChatApp.Core.Entities;
using ChatApp.Core.Enums;
using ChatApp.Core.Exceptions;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;

namespace ChatApp.Application.Services;

/// <summary>
/// V1: manages the call record lifecycle.
/// WebRTC SDP/ICE signaling is relayed peer-to-peer via VideoHub — 
/// this service only tracks state in the DB.
///
/// V2 swap: implement IVideoCallService with LiveKitService,
/// which will issue LiveKit room tokens here instead of managing raw SDP relay.
/// </summary>
public class VideoCallService(
    IVideoCallRepository callRepo,
    IUserRepository userRepo) : IVideoCallService
{
    public async Task<VideoCallDto> InitiateCallAsync(InitiateCallRequest request, Guid callerId, CancellationToken ct = default)
    {
        var caller = await userRepo.GetByIdAsync(callerId, ct)
            ?? throw new NotFoundException(nameof(AppUser), callerId);

        var callee = await userRepo.GetByIdAsync(request.CalleeId, ct)
            ?? throw new NotFoundException(nameof(AppUser), request.CalleeId);

        // Prevent multiple simultaneous calls
        var existingCall = await callRepo.GetActiveCallForUserAsync(callerId, ct);
        if (existingCall is not null)
            throw new CallAlreadyActiveException(callerId);

        var call = new VideoCall
        {
            CallerId = callerId,
            CalleeId = request.CalleeId,
            RoomId = request.RoomId,
            Status = CallStatus.Pending
        };

        var created = await callRepo.AddAsync(call, ct);
        return ToDto(created, caller, callee);
    }

    public async Task<VideoCallDto> AcceptCallAsync(Guid callId, Guid calleeId, CancellationToken ct = default)
    {
        var call = await callRepo.GetByIdAsync(callId, ct)
            ?? throw new CallNotFoundException(callId);

        if (call.CalleeId != calleeId)
            throw new UnauthorizedException("Only the callee can accept this call.");

        if (call.Status != CallStatus.Pending)
            throw new InvalidCallStateException($"Call cannot be accepted in state '{call.Status}'.");

        call.Status = CallStatus.Active;
        call.AnsweredAt = DateTime.UtcNow;

        await callRepo.UpdateAsync(call, ct);

        var caller = await userRepo.GetByIdAsync(call.CallerId, ct)!;
        var callee = await userRepo.GetByIdAsync(call.CalleeId, ct)!;
        return ToDto(call, caller!, callee!);
    }

    public async Task EndCallAsync(Guid callId, Guid requestingUserId, CancellationToken ct = default)
    {
        var call = await callRepo.GetByIdAsync(callId, ct)
            ?? throw new CallNotFoundException(callId);

        bool isParticipant = call.CallerId == requestingUserId || call.CalleeId == requestingUserId;
        if (!isParticipant)
            throw new UnauthorizedException("Only call participants can end the call.");

        call.Status = CallStatus.Ended;
        call.EndedAt = DateTime.UtcNow;

        await callRepo.UpdateAsync(call, ct);
    }

    public async Task RejectCallAsync(Guid callId, Guid calleeId, CancellationToken ct = default)
    {
        var call = await callRepo.GetByIdAsync(callId, ct)
            ?? throw new CallNotFoundException(callId);

        if (call.CalleeId != calleeId)
            throw new UnauthorizedException("Only the callee can reject this call.");

        if (call.Status != CallStatus.Pending)
            throw new InvalidCallStateException($"Call cannot be rejected in state '{call.Status}'.");

        call.Status = CallStatus.Rejected;
        call.EndedAt = DateTime.UtcNow;

        await callRepo.UpdateAsync(call, ct);
    }

    public async Task<VideoCallDto?> GetActiveCallAsync(Guid userId, CancellationToken ct = default)
    {
        var call = await callRepo.GetActiveCallForUserAsync(userId, ct);
        if (call is null) return null;

        var caller = await userRepo.GetByIdAsync(call.CallerId, ct)!;
        var callee = await userRepo.GetByIdAsync(call.CalleeId, ct)!;
        return ToDto(call, caller!, callee!);
    }

    public async Task<IReadOnlyList<VideoCallDto>> GetCallHistoryAsync(Guid userId, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var calls = await callRepo.GetCallHistoryAsync(userId, skip, take, ct);
        var result = new List<VideoCallDto>();

        foreach (var call in calls)
        {
            var caller = await userRepo.GetByIdAsync(call.CallerId, ct)!;
            var callee = await userRepo.GetByIdAsync(call.CalleeId, ct)!;
            result.Add(ToDto(call, caller!, callee!));
        }

        return result;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static VideoCallDto ToDto(VideoCall call, AppUser caller, AppUser callee) => new(
        call.Id,
        call.Status,
        call.CallerId,
        caller.DisplayName,
        call.CalleeId,
        callee.DisplayName,
        call.InitiatedAt,
        call.AnsweredAt,
        call.EndedAt
    );
}
