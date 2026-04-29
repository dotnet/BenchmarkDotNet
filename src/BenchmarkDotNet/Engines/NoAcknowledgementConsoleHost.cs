using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Engines;

// This class is used when somebody manually launches benchmarking .exe without providing ipc connection.
[AggressivelyOptimizeMethods]
public sealed class NoAcknowledgementConsoleHost : IHost
{
    private readonly TextWriter outWriter;

    public CancellationToken CancellationToken => CancellationToken.None;

    public NoAcknowledgementConsoleHost() => outWriter = Console.Out;

    public void Dispose()
    {
        // do nothing on purpose - there is no point in closing STD OUT
    }

    public void WriteLine()
        => outWriter.WriteLine();

    public void WriteLine(string message)
        => outWriter.WriteLine(message);

    public void SendError(string message)
        => WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public void ReportResults(RunResults runResults)
        => runResults.Print(this);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
        => WriteLine(Engine.Signals.ToMessage(hostSignal));

    public ValueTask Yield() => new();
}
