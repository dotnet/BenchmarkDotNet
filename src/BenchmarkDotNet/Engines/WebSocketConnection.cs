using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;

namespace BenchmarkDotNet.Engines;

internal sealed class WebSocketConnection(WebSocket socket) : IpcConnection
{
    public override void Dispose()
        => socket.Dispose();

    internal override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        => await socket.ReadStringAsync(cancellationToken);

    internal override async ValueTask WriteLineAsync(string line)
        => await socket.WriteStringAsync(line);
}
