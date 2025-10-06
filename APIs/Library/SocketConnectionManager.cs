using Common.Models;
using Entities.Objects;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Library;

public class SocketConnectionManager
{
    private readonly List<(WebSocket socket, uint userId)> _sockets = new();

    public void AddSocket(WebSocket socket, uint userId)
    {
        lock (_sockets) _sockets.Add((socket, userId));
    }

    public async Task RemoveSocket(WebSocket socket)
    {
        lock (_sockets) _sockets.RemoveAll(x => x.socket == socket);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
    }

    public async Task BroadcastEventAsync(string eventType, IEnumerable<uint> memberUserIds, string msg)
    {
        var wsMsg = new WsEvent{ Event = eventType, Msg = msg };
        var json = JsonSerializer.Serialize(wsMsg);
        var buffer = Encoding.UTF8.GetBytes(json);

        List<(WebSocket socket, uint userId)> socketsCopy;
        lock (_sockets)
            socketsCopy = _sockets.ToList();

        foreach (var (socket, userId) in socketsCopy)
        {
            if (socket.State == WebSocketState.Open && memberUserIds.Contains(userId))
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

public class WsEvent
{
    public string Event { get; set; } = null!;
    public string Msg { get; set; } = null!;
}
