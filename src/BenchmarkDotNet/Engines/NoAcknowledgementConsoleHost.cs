using System;
using System.IO;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Engines;

// This class is used when somebody manually launches benchmarking .exe without providing pipe name, or for wasm that doesn't support pipes.
[AggressivelyOptimizeMethods]
internal sealed class NoAcknowledgementConsoleHost : IHost
{
    private readonly TextWriter outWriter;

    public NoAcknowledgementConsoleHost() => outWriter = Console.Out;

    public void WriteAsync(string message)
        => outWriter.Write(message);

    public void WriteLine()
        => outWriter.WriteLine();

    public void WriteLine(string message)
        => outWriter.WriteLine(message);

    public void SendSignal(HostSignal hostSignal)
        => WriteLine(Engine.Signals.ToMessage(hostSignal));

    public void SendError(string message)
        => WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public void ReportResults(RunResults runResults)
        => runResults.Print(outWriter);

    public void Dispose()
    {
        // do nothing on purpose - there is no point in closing STD OUT
    }
}
