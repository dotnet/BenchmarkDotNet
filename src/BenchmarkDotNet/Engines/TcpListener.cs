using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal sealed class TcpListener : IpcListener
{
    private readonly System.Net.Sockets.TcpListener listener = new(IPAddress.Loopback, port: 0);

    public override void Dispose()
        => listener.Stop();

    internal int StartAndGetPort()
    {
        listener.Start(1);
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    internal override async ValueTask<IpcConnection> AcceptConnection(CancellationToken cancellationToken)
    {
        TcpClient client;
#if NET6_0_OR_GREATER
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(IpcHelper.ConnectionTimeout);
        try
        {
            client = await listener.AcceptTcpClientAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The connection to the benchmark process timed out after {IpcHelper.ConnectionTimeout}.");
        }
#else
        client = await listener.AcceptTcpClientAsync().WaitAsync(IpcHelper.ConnectionTimeout, cancellationToken);
#endif
        return new TcpConnection(client);
    }
}
