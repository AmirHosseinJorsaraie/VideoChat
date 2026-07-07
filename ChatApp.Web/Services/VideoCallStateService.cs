using ChatApp.Core.DTOs;
using ChatApp.Core.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;

namespace ChatApp.Web.Services;

/// <summary>
/// Manages the VideoHub SignalR connection and the WebRTC call state.
/// Components subscribe to events here instead of polling.
///
/// WebRTC JS interop is handled via wwwroot/js/webrtc.js.
/// </summary>
public class VideoCallStateService(
    IJSRuntime js,
    NavigationManager navigationManager,
    IHttpContextAccessor httpContextAccessor) : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private DotNetObjectReference<VideoCallStateService>? _dotNetRef;

    public VideoCallDto? CurrentCall { get; private set; }
    public bool IsInCall => CurrentCall is not null;
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string? LastError { get; private set; }

    // Components subscribe to trigger UI updates
    public event Action<VideoCallDto>? OnIncomingCall;
    public event Action<Guid>? OnCallAccepted;
    public event Action<Guid>? OnCallRejected;
    public event Action<Guid>? OnCallEnded;
    public event Action? OnStateChanged;

    public async Task ConnectAsync(string hubUrl)
    {
        if (_hubConnection is not null)
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
                await _hubConnection.StartAsync();

            return;
        }

        var absoluteHubUrl = navigationManager.ToAbsoluteUri(hubUrl);
        _dotNetRef = DotNetObjectReference.Create(this);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(absoluteHubUrl, options =>
            {
                options.UseDefaultCredentials = true;
                AddCookieHeader(options);
            })
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
            if (CurrentCall?.Id == callId)
                CurrentCall = CurrentCall with { Status = CallStatus.Active };

            OnCallAccepted?.Invoke(callId);
            OnStateChanged?.Invoke();

            try
            {
                await js.InvokeVoidAsync("webRTC.createOffer", callId, _dotNetRef);
            }
            catch (JSException ex)
            {
                LastError = ex.Message;
                OnStateChanged?.Invoke();
            }
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
            try
            {
                await js.InvokeVoidAsync("webRTC.handleOffer", signal, _dotNetRef);
            }
            catch (JSException ex)
            {
                LastError = ex.Message;
                OnStateChanged?.Invoke();
            }
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
        LastError = null;
        CurrentCall = await _hubConnection.InvokeAsync<VideoCallDto>("InitiateCall", calleeId, roomId);
        OnStateChanged?.Invoke();
    }

    public async Task AcceptCallAsync(Guid callId)
    {
        if (_hubConnection is null) return;
        LastError = null;
        CurrentCall = await _hubConnection.InvokeAsync<VideoCallDto>("AcceptCall", callId);
        OnStateChanged?.Invoke();
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

        _dotNetRef?.Dispose();
    }

    private void AddCookieHeader(HttpConnectionOptions options)
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie))
            options.Headers["Cookie"] = cookie;
    }
}
