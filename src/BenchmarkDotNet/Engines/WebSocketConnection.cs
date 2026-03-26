using vtortola.WebSockets;

namespace BenchmarkDotNet.Engines;

internal sealed class WebSocketConnection(WebSocket socket) : IpcConnection
{
    public override void Dispose()
        => socket.Dispose();

    internal override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        => new(socket.ReadStringAsync(cancellationToken));

    internal override ValueTask WriteLineAsync(string line)
        => new(socket.WriteStringAsync(line));
}
