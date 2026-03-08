using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal abstract class IpcConnection : IDisposable
{
    public abstract void Dispose();
    internal abstract ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken);
    // Write sync because this is only used to send short messages to the child process.
    internal abstract void WriteLine(string line);
}
