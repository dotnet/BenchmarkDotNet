using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Loggers;

internal sealed class StreamWriterWrapper(CancelableStreamWriter writer) : StreamOrLoggerWriter
{
    public override ValueTask WriteLineAsync(CancellationToken cancellationToken)
        => new(writer.WriteLineAsync(cancellationToken));

    public override ValueTask WriteLineAsync(string line, LogKind logKind, CancellationToken cancellationToken)
        => new(writer.WriteLineAsync(line, cancellationToken));

    public override ValueTask WriteAsync(string line, LogKind logKind, CancellationToken cancellationToken)
        => new(writer.WriteAsync(line, cancellationToken));
}
