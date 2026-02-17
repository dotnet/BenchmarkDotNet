using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal abstract class IpcListener : IDisposable
{
    public abstract void Dispose();
    internal abstract ValueTask<IpcConnection> AcceptConnection(CancellationToken cancellationToken);
}
