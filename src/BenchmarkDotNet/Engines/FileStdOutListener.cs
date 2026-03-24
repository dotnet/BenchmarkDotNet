using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;

namespace BenchmarkDotNet.Engines;

internal sealed class FileStdOutListener(string directory) : IpcListener
{
    private readonly string ipcDirectory = Path.Combine(directory, "ipc");
    private AsyncProcessOutputReader? processOutputReader;

    public override void Dispose()
    {
        // Do nothing, ipc files will be cleaned up by the benchmark infrastructure.
    }

    internal string GetIpcDirectory() => ipcDirectory;

    internal void AttachProcessOutputReader(AsyncProcessOutputReader processOutputReader)
    {
        this.processOutputReader = processOutputReader;
    }

    internal override ValueTask<IpcConnection> AcceptConnection(CancellationToken cancellationToken)
    {
        if (processOutputReader == null)
            throw new InvalidOperationException("ProcessOutputReader must be attached before accepting connection.");

        // Clean up IPC directory in case a previous benchmark run kept benchmark files.
        try
        {
            if (Directory.Exists(ipcDirectory))
                Directory.Delete(ipcDirectory, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors - best effort
        }
        Directory.CreateDirectory(ipcDirectory);

        return new(new FileStdOutConnection(processOutputReader, ipcDirectory));
    }
}
