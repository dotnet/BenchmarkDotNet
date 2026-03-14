using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Validators;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
internal sealed class TcpHost : IHost
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly StreamWriter outWriter;
    private readonly StreamReader inReader;
    private readonly Task readTask;
    private TaskCompletionSource<string?>? acknowledgmentSource;

    public CancellationToken CancellationToken => cancellationTokenSource.Token;

    public TcpHost(TcpClient client)
    {
        var stream = client.GetStream();
        // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
        outWriter = new(stream, IpcHelper.UTF8NoBOM) { AutoFlush = true };
        inReader = new(stream, IpcHelper.UTF8NoBOM, detectEncodingFromByteOrderMarks: false);

        // Start task to monitor for messages.
        readTask = ReceiveMessages();
    }

    private async Task ReceiveMessages()
    {
        while (true)
        {
            string? message = await inReader.ReadLineAsync().ConfigureAwait(false);

            if (message == null)
            {
                // Stream closed
                break;
            }

            var source = acknowledgmentSource;
            acknowledgmentSource = null;

            if (message == "CANCEL")
            {
                cancellationTokenSource.Cancel();
                source?.SetCanceled();
                break;
            }

            source?.SetResult(message);
        }
    }

    public void Dispose()
    {
        outWriter.Dispose();
        inReader.Dispose();
        cancellationTokenSource.Dispose();
        readTask.GetAwaiter().GetResult();
    }

    public void WriteLine()
        => outWriter.WriteLine();

    public void WriteLine(string message)
        => outWriter.WriteLine(message);

    public void SendError(string message)
        => outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public void ReportResults(RunResults runResults)
        => runResults.Print(this);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
    {
        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            Thread.Sleep(1);
        }

        var source = new TaskCompletionSource<string?>();
        acknowledgmentSource = source;

        outWriter.WriteLine(Engine.Signals.ToMessage(hostSignal));

        string? acknowledgment = await source.Task;
        if (acknowledgment != Engine.Signals.Acknowledgment
            && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
        {
            throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }
    }

    public ValueTask Yield() => new();
}
