namespace BenchmarkDotNet.Loggers;

internal sealed class PrefixedStreamOrLoggerWriter(StreamOrLoggerWriter inner, string prefix) : StreamOrLoggerWriter
{
    private bool isNewLine = true;

    public override ValueTask WriteLineAsync(CancellationToken cancellationToken)
    {
        isNewLine = true;
        return inner.WriteLineAsync(cancellationToken);
    }

    public override async ValueTask WriteLineAsync(string line, LogKind logKind, CancellationToken cancellationToken)
    {
        if (isNewLine && line.Length > 0)
            await inner.WriteAsync(prefix, logKind, cancellationToken).ConfigureAwait(false);
        await inner.WriteLineAsync(line, logKind, cancellationToken).ConfigureAwait(false);
        isNewLine = true;
    }

    public override async ValueTask WriteAsync(string line, LogKind logKind, CancellationToken cancellationToken)
    {
        if (isNewLine && line.Length > 0)
            await inner.WriteAsync(prefix, logKind, cancellationToken).ConfigureAwait(false);
        await inner.WriteAsync(line, logKind, cancellationToken).ConfigureAwait(false);
        isNewLine = false;
    }
}
