using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Engines;

internal sealed class FileStdOutConnection(AsyncProcessOutputReader processOutputReader, string ipcDirectory) : IpcConnection
{
    private int ackCounter = 0;

    public override void Dispose()
    {
        // Do nothing, files will be cleaned up by the benchmark infrastructure.
    }

    internal override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        // Read from process stdout via AsyncProcessOutputReader
        return await processOutputReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
    }

    internal override async ValueTask WriteLineAsync(string line)
    {
        // Write acknowledgment to a file that the child can read
        var ackFile = Path.Combine(ipcDirectory, $"ack-{ackCounter++}.txt");

        // Write to temp file first, then move (atomic operation)
        var tempFile = ackFile + ".tmp";
#if NETSTANDARD2_0
        await Task.Run(() => File.WriteAllText(tempFile, line, Encoding.UTF8));
#else
        await File.WriteAllTextAsync(tempFile, line, Encoding.UTF8);
#endif
        File.Move(tempFile, ackFile);
    }
}
