using BenchmarkDotNet.Helpers;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal sealed class TcpConnection : IpcConnection
{
    private readonly NetworkStream stream;
    private readonly CancelableStreamReader reader;
    private readonly StreamWriter writer;

    internal TcpConnection(TcpClient client)
    {
        stream = client.GetStream();
        reader = new(stream, IpcHelper.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
        // Flush the data to the Stream after each write, otherwise the client will wait for input endlessly!
        writer = new(stream, IpcHelper.UTF8NoBOM, bufferSize: 1) { AutoFlush = true };
    }

    public override void Dispose()
    {
        writer.Dispose();
        reader.Dispose();
        stream.Dispose();
    }

    internal override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        => await reader.ReadLineAsync(cancellationToken);

    internal override async ValueTask WriteLineAsync(string line)
        // Write sync because this is only used to send short messages to the child process.
        => writer.WriteLine(line);
}
