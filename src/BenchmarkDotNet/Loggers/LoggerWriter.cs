using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Loggers;

internal sealed class LoggerWriter(ILogger logger) : StreamOrLoggerWriter
{
    public override ValueTask WriteLineAsync(string line, LogKind logKind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.WriteLine(logKind, line);
        return new();
    }

    public override ValueTask WriteAsync(string line, LogKind logKind, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.Write(logKind, line);
        return new();
    }
}
