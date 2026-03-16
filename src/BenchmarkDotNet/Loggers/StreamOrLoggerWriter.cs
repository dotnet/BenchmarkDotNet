using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Loggers;

internal abstract class StreamOrLoggerWriter
{
    public abstract ValueTask WriteLineAsync(CancellationToken cancellationToken);
    public ValueTask WriteLineAsync(string line, CancellationToken cancellationToken) => WriteLineAsync(line, LogKind.Default, cancellationToken);
    public abstract ValueTask WriteLineAsync(string line, LogKind logKind, CancellationToken cancellationToken);
    public ValueTask WriteAsync(string line, CancellationToken cancellationToken) => WriteAsync(line, LogKind.Default, cancellationToken);
    public abstract ValueTask WriteAsync(string line, LogKind logKind, CancellationToken cancellationToken);
}
