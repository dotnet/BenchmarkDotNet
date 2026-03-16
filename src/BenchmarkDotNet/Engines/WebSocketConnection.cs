using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace BenchmarkDotNet.Engines;

internal sealed class WebSocketConnection(WebSocket socket) : IpcConnection
{
    public override void Dispose()
        => socket.Dispose();

    internal override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        => new(socket.ReadStringAsync(cancellationToken));

    internal override void WriteLine(string line)
        // vtortola WebSocket does not support sync writes, we have to do sync-over-async.
        => socket.WriteStringAsync(line).GetAwaiter().GetResult();
}
