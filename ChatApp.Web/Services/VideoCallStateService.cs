using ChatApp.Core.DTOs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace ChatApp.Web.Services;

/// <summary>
/// Manages the VideoHub SignalR connection and the WebRTC call state.
/// Components subscribe to events here instead of polling.
///
/// WebRTC JS interop is handled via wwwroot/js/webrtc.js.
/// </summary>
public class VideoCallStateService(IJSRuntime js) : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public VideoCallDto? CurrentCall { get; private set; }
    public bool IsInCall => CurrentCall is not null;

    // Components subscribe to trigger UI updates
    public event Action<VideoCallDto>? OnIncomingCall;
    public event Action<Guid>? OnCallAccepted;
    public event Action<Guid>? OnCallRejected;
    public event Action<Guid>? OnCallEnded;
    public event Action? OnStateChanged;

    public async Task ConnectAsync(string hubUrl)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.UseDefaultCredentials = true; })
            .WithAutomaticReconnect()
            .Build();

        // ── Incoming call ─────────────────────────────────────────────────────
        _hubConnection.On<VideoCallDto>("IncomingCall", call =>
        {
            CurrentCall = call;
            OnIncomingCall?.Invoke(call);
            OnStateChanged?.Invoke();
        });

        // ── Call accepted → start WebRTC as caller (send offer) ───────────────
        _hubConnection.On<Guid>("CallAccepted", async callId =>
        {
            OnCallAccepted?.Invoke(callId);
            // Caller creates the SDP offer and sends it via hub
            await js.InvokeVoidAsync("webRTC.createOffer", callId,
                DotNetObjectReference.Create(this));
        });

        _hubConnection.On<Guid>("CallRejected", callId =>
        {
            CurrentCall = null;
            OnCallRejected?.Invoke(callId);
            OnStateChanged?.Invoke();
        });

        _hubConnection.On<Guid>("CallEnded", async callId =>
        {
            CurrentCall = null;
            await js.InvokeVoidAsync("webRTC.closeConnection");
            OnCallEnded?.Invoke(callId);
            OnStateChanged?.Invoke();
        });

        // ── WebRTC signaling relay ─────────────────────────────────────────────
        _hubConnection.On<SdpSignalDto>("ReceiveOffer", async signal =>
        {
            // Callee receives offer → create answer
            await js.InvokeVoidAsync("webRTC.handleOffer", signal,
                DotNetObjectReference.Create(this));
        });

        _hubConnection.On<SdpSignalDto>("ReceiveAnswer", async signal =>
        {
            await js.InvokeVoidAsync("webRTC.handleAnswer", signal);
        });

        _hubConnection.On<IceCandidateDto>("ReceiveIceCandidate", async candidate =>
        {
            await js.InvokeVoidAsync("webRTC.handleIceCandidate", candidate);
        });

        await _hubConnection.StartAsync();
    }

    // ── Call actions ──────────────────────────────────────────────────────────

    public async Task InitiateCallAsync(Guid calleeId, Guid? roomId = null)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("InitiateCall", calleeId, roomId);
    }

    public async Task AcceptCallAsync(Guid callId)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("AcceptCall", callId);
    }

    public async Task RejectCallAsync(Guid callId)
    {
        if (_hubConnection is null) return;
        CurrentCall = null;
        await _hubConnection.InvokeAsync("RejectCall", callId);
        OnStateChanged?.Invoke();
    }

    public async Task EndCallAsync(Guid callId)
    {
        if (_hubConnection is null) return;
        await _hubConnection.InvokeAsync("EndCall", callId);
        await js.InvokeVoidAsync("webRTC.closeConnection");
        CurrentCall = null;
        OnStateChanged?.Invoke();
    }

    // ── JS-invokable: called by webrtc.js to relay signaling through hub ──────

    [JSInvokable]
    public async Task SendOfferAsync(SdpSignalDto signal)
    {
        if (_hubConnection is not null)
            await _hubConnection.InvokeAsync("SendOffer", signal);
    }

    [JSInvokable]
    public async Task SendAnswerAsync(SdpSignalDto signal)
    {
        if (_hubConnection is not null)
            await _hubConnection.InvokeAsync("SendAnswer", signal);
    }

    [JSInvokable]
    public async Task SendIceCandidateAsync(IceCandidateDto candidate)
    {
        if (_hubConnection is not null)
            await _hubConnection.InvokeAsync("SendIceCandidate", candidate);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
