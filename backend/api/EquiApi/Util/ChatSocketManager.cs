using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EquiApi.Util;

/// <summary>
///     Keeps the open chat websockets of all persons and pushes chat events to them.
///     A person can be connected several times (e.g. on the phone and in the browser).
/// </summary>
public interface IChatSocketManager
{
    /// <summary>
    ///     Keeps the websocket open until the client closes it or the request is aborted
    /// </summary>
    /// <param name="personId">The id of the connected person</param>
    /// <param name="socket">The accepted websocket</param>
    /// <param name="cancellationToken">Aborts the connection</param>
    public Task HandleConnectionAsync(int personId, WebSocket socket, CancellationToken cancellationToken);

    /// <summary>
    ///     Sends an event to all open websockets of the given persons
    /// </summary>
    /// <param name="personIds">The ids of the persons to notify</param>
    /// <param name="payload">The event, serialized as JSON</param>
    public Task SendAsync(IEnumerable<int> personIds, object payload);
}

internal sealed class ChatSocketManager(IOptions<JsonOptions> jsonOptions, ILogger<ChatSocketManager> logger)
    : IChatSocketManager
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, Connection>> _connections = new();

    public async Task HandleConnectionAsync(int personId, WebSocket socket, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var connection = new Connection(socket);
        _connections.GetOrAdd(personId, _ => new ConcurrentDictionary<Guid, Connection>())[id] = connection;
        logger.LogInformation("Chat websocket of person {PersonId} opened", personId);

        var buffer = new byte[1024];
        try
        {
            // the clients only listen, incoming data (e.g. pings) is ignored
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                }
            }
        }
        catch (Exception e) when (e is OperationCanceledException or WebSocketException)
        {
            // the client disconnected without closing the socket
        }
        finally
        {
            if (_connections.TryGetValue(personId, out var connections))
            {
                connections.TryRemove(id, out _);
            }

            logger.LogInformation("Chat websocket of person {PersonId} closed", personId);
        }
    }

    public async Task SendAsync(IEnumerable<int> personIds, object payload)
    {
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(payload, jsonOptions.Value.JsonSerializerOptions);
        var targets = personIds.Distinct()
                               .SelectMany(personId => _connections.TryGetValue(personId, out var connections)
                                                           ? connections.Values
                                                           : [])
                               .ToList();

        await Task.WhenAll(targets.Select(connection => connection.SendAsync(data)));
    }

    private sealed class Connection(WebSocket socket)
    {
        // a websocket allows only one send operation at a time
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public async Task SendAsync(byte[] data)
        {
            await _sendLock.WaitAsync();
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                // the connection is gone, it is removed by its receive loop
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}