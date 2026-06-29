using ChatApp.Core.Enums;

namespace ChatApp.Core.DTOs;

// ── Call lifecycle ────────────────────────────────────────────────────────────

public record VideoCallDto(
    Guid Id,
    CallStatus Status,
    Guid CallerId,
    string CallerDisplayName,
    Guid CalleeId,
    string CalleeDisplayName,
    DateTime InitiatedAt,
    DateTime? AnsweredAt,
    DateTime? EndedAt
);

public record InitiateCallRequest(
    Guid CalleeId,
    Guid? RoomId  // optional — null means direct call from profile
);

// ── WebRTC signaling DTOs ─────────────────────────────────────────────────────
// These are relayed through VideoHub between the two peers.
// The Hub never inspects the payload — it just routes it to the other party.
// This design means V2 (multi-party) only needs to change the routing logic,
// not these DTOs.

public record SdpSignalDto(
    Guid CallId,
    string Type,   // "offer" or "answer"
    string Sdp     // Session Description Protocol payload
);

public record IceCandidateDto(
    Guid CallId,
    string Candidate,
    string? SdpMid,
    int? SdpMLineIndex
);
