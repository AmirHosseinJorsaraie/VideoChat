using ChatApp.Core.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatApp.Web.Services;

/// <summary>
/// Scoped per Blazor circuit. Holds the SignalR connection for chat,
/// current room state, and message list. Components inject this and
/// subscribe to its events to trigger StateHasChanged().
/// </summary>
public class ChatStateService : IAsyncDisposable
{
    private HubConnection? _hubConnection;

    public RoomDto? CurrentRoom { get; private set; }
    public List<MessageDto> Messages { get; } = [];
    public int ViewerCount { get; private set; }

    // Components subscribe to these to re-render
    public event Action? OnStateChanged;
    public event Action<MessageDto>? OnMessageReceived;
    public event Action? OnStreamEnded;

    public async Task ConnectAsync(string hubUrl, string accessToken)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Cookie auth is handled automatically by the browser;
                // this ensures reconnects also carry credentials
                options.UseDefaultCredentials = true;
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
            OnStreamEnded?.Invoke();
            OnStateChanged?.Invoke();
        });

        await _hubConnection.StartAsync();
    }

    public async Task JoinRoomAsync(RoomDto room)
    {
        CurrentRoom = room;
        Messages.Clear();

        if (_hubConnection is not null)
            await _hubConnection.InvokeAsync("JoinRoom", room.Id);

        OnStateChanged?.Invoke();
    }

    public async Task LeaveRoomAsync()
    {
        if (_hubConnection is not null && CurrentRoom is not null)
            await _hubConnection.InvokeAsync("LeaveRoom", CurrentRoom.Id);

        CurrentRoom = null;
        Messages.Clear();
        OnStateChanged?.Invoke();
    }

    public async Task SendMessageAsync(string content)
    {
        if (_hubConnection is null || CurrentRoom is null) return;
        await _hubConnection.InvokeAsync("SendMessage", CurrentRoom.Id, content);
    }

    public async Task DeleteMessageAsync(Guid messageId)
    {
        if (_hubConnection is null || CurrentRoom is null) return;
        await _hubConnection.InvokeAsync("DeleteMessage", messageId, CurrentRoom.Id);
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
            await _hubConnection.DisposeAsync();
    }
}
