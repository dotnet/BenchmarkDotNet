using BenchmarkDotNet.Helpers;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace BenchmarkDotNet.Engines;

internal sealed class WebSocketListener : IpcListener
{
    private readonly vtortola.WebSockets.WebSocketListener listener;

    public WebSocketListener()
    {
        var options = new WebSocketListenerOptions();
        options.Standards.RegisterRfc6455();
        listener = new(new IPEndPoint(IPAddress.Loopback, port: 0), options);
    }

    public override void Dispose()
        => listener.Dispose();

    internal async ValueTask<int> StartAndGetPortAsync()
    {
        await listener.StartAsync();
        return ((IPEndPoint)listener.LocalEndpoints.First()).Port;
    }

    internal override async ValueTask<IpcConnection> AcceptConnection(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(IpcHelper.ConnectionTimeout);
        try
        {
            var webSocket = await listener.AcceptWebSocketAsync(cancellationToken);
            return new WebSocketConnection(webSocket);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The connection to the benchmark process timed out after {IpcHelper.ConnectionTimeout}.");
        }
    }
}
