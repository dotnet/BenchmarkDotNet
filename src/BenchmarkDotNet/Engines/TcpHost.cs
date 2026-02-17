using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
internal sealed class TcpHost : IHost
{
    private readonly StreamWriter outWriter;
    private readonly StreamReader inReader;

    public TcpHost(TcpClient client)
    {
        var stream = client.GetStream();
        // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
        outWriter = new(stream, IpcHelper.UTF8NoBOM) { AutoFlush = true };
        inReader = new(stream, IpcHelper.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
    }

    public void Dispose()
    {
        outWriter.Dispose();
        inReader.Dispose();
    }

    public void WriteLine()
        => outWriter.WriteLine();

    public void WriteLine(string message)
        => outWriter.WriteLine(message);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
    {
        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            Thread.Sleep(1);
        }

        outWriter.WriteLine(Engine.Signals.ToMessage(hostSignal));

        // Read the response from Parent process.
        string? acknowledgment = inReader.ReadLine();
        if (acknowledgment != Engine.Signals.Acknowledgment
            && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
        {
            throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }
    }

    public void SendError(string message)
        => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public void ReportResults(RunResults runResults)
        => runResults.Print(this);
}
