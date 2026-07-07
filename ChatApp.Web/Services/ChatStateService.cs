using ChatApp.Core.DTOs;
using ChatApp.Core.Enums;
using ChatApp.Core.Interfaces.Repositories;
using ChatApp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Web.Services;

/// <summary>
/// Scoped per Blazor circuit. Holds the SignalR connection for chat,
/// current room state, and message list. Components inject this and
/// subscribe to its events to trigger StateHasChanged().
/// </summary>
public class ChatStateService(
    NavigationManager navigationManager,
    IHttpContextAccessor httpContextAccessor,
    IMessageService messageService) : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private bool _disposed;

    public RoomDto? CurrentRoom { get; private set; }
    public List<MessageDto> Messages { get; } = [];
    public int ViewerCount { get; private set; }
    public bool CanSendMessages => IsConnected && CurrentRoom?.IsLive == true;

    // Components subscribe to these to re-render
    public event Action? OnStateChanged;
    public event Action<MessageDto>? OnMessageReceived;
    public event Action? OnStreamEnded;

    public async Task ConnectAsync(string hubUrl, string accessToken)
    {
        if (_disposed) return;

        if (_hubConnection is not null)
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
                await _hubConnection.StartAsync();

            return;
        }

        var absoluteHubUrl = navigationManager.ToAbsoluteUri(hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(absoluteHubUrl, options =>
            {
                // Cookie auth is handled automatically by the browser;
                // this ensures reconnects also carry credentials
                options.UseDefaultCredentials = true;
                AddCookieHeader(options);
            })
            .WithAutomaticReconnect()
            .Build();

        // Register client-side handlers
        _hubConnection.On<MessageDto>("ReceiveMessage", msg =>
        {
            Messages.Add(msg);
            OnMessageReceived?.Invoke(msg);
            OnStateChanged?.Invoke();
        });

        _hubConnection.On<Guid>("MessageDeleted", id =>
        {
            Messages.RemoveAll(m => m.Id == id);
            OnStateChanged?.Invoke();
        });

        _hubConnection.On<int>("ViewerCountUpdated", count =>
        {
            ViewerCount = count;
            OnStateChanged?.Invoke();
        });

        _hubConnection.On<Guid>("StreamStarted", _ => OnStateChanged?.Invoke());
        _hubConnection.On<Guid>("StreamEnded", _ =>
        {
            if (CurrentRoom is not null)
                CurrentRoom = CurrentRoom with { Status = RoomStatus.Ended, EndedAt = DateTime.UtcNow };

            OnStreamEnded?.Invoke();
            OnStateChanged?.Invoke();
        });

        await _hubConnection.StartAsync();
    }

    public async Task LoadChatHistory(int skip = 0, int take = 50)
    {
        if (CurrentRoom is null) return;

        var messages = await messageService.GetRoomHistoryAsync(CurrentRoom.Id, skip, take);
        Messages.AddRange(messages);
    }

    public async Task JoinRoomAsync(RoomDto room)
    {
        if (_disposed) return;

        CurrentRoom = room;
        Messages.Clear();
        await LoadChatHistory();

        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.InvokeAsync("JoinRoom", room.Id);

        OnStateChanged?.Invoke();
    }

    public async Task LeaveRoomAsync()
    {
        var roomId = CurrentRoom?.Id;

        CurrentRoom = null;
        Messages.Clear();
        if (!_disposed)
            OnStateChanged?.Invoke();

        if (_disposed || roomId is null || _hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync("LeaveRoom", roomId.Value);
        }
        catch (ObjectDisposedException)
        {
            // Navigation/circuit shutdown can dispose the connection before page disposal finishes.
        }
        catch (InvalidOperationException)
        {
            // The connection may transition away from Connected between the state check and invoke.
        }
    }

    public async Task SendMessageAsync(string content)
    {
        var connection = _hubConnection;
        if (_disposed || !CanSendMessages || CurrentRoom is null || connection is null) return;

        await connection.InvokeAsync("SendMessage", CurrentRoom.Id, content);
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        if (_disposed || _hubConnection?.State != HubConnectionState.Connected || CurrentRoom is null) return;
        await _hubConnection.InvokeAsync("DeleteMessage", messageId, CurrentRoom.Id);
    }

    public async Task NotifyStreamEndedAsync(Guid roomId)
    {
        if (_disposed || _hubConnection?.State != HubConnectionState.Connected) return;

        try
        {
            await _hubConnection.InvokeAsync("NotifyStreamEnded", roomId);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    private void AddCookieHeader(HttpConnectionOptions options)
    {
        var cookie = httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(cookie))
            options.Headers["Cookie"] = cookie;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        if (_hubConnection is null) return;

        try
        {
            await _hubConnection.DisposeAsync();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
