using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Loggers;

// Necessary because ILogger doesn't support async and StreamWriter doesn't support async writes with CancellationToken in netstandard2.0.
public abstract class StreamOrLoggerWriter
{
    public ValueTask WriteLineAsync(CancellationToken cancellationToken) => WriteLineAsync(string.Empty, cancellationToken);
    public ValueTask WriteLineAsync(string line, CancellationToken cancellationToken) => WriteLineAsync(line, LogKind.Default, cancellationToken);
    public abstract ValueTask WriteLineAsync(string line, LogKind logKind, CancellationToken cancellationToken);
    public ValueTask WriteAsync(string line, CancellationToken cancellationToken) => WriteAsync(line, LogKind.Default, cancellationToken);
    public abstract ValueTask WriteAsync(string line, LogKind logKind, CancellationToken cancellationToken);
}
