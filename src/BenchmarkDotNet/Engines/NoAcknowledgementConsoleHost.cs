using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Engines;

// This class is used when somebody manually launches benchmarking .exe without providing pipe name, or for wasm that doesn't support pipes.
[AggressivelyOptimizeMethods]
internal sealed class NoAcknowledgementConsoleHost : IHost
{
    private readonly TextWriter outWriter;

    public NoAcknowledgementConsoleHost() => outWriter = Console.Out;

    public async ValueTask WriteAsync(string message)
        => await outWriter.WriteAsync(message);

    public async ValueTask WriteLineAsync()
        => await outWriter.WriteLineAsync();

    public async ValueTask WriteLineAsync(string message)
        => await outWriter.WriteLineAsync(message);

    public ValueTask SendSignalAsync(HostSignal hostSignal)
        => WriteLineAsync(Engine.Signals.ToMessage(hostSignal));

    public ValueTask SendErrorAsync(string message)
        => WriteLineAsync($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public ValueTask ReportResultsAsync(RunResults runResults)
        => runResults.WriteAsync(this);

    public void Dispose()
    {
        // do nothing on purpose - there is no point in closing STD OUT
    }
}
