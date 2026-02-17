using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal abstract class IpcConnection : IDisposable
{
    public abstract void Dispose();
    internal abstract ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken);
    internal abstract ValueTask WriteLineAsync(string line);
}
