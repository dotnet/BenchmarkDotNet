using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public class NamedPipeHost : IHost
{
    internal const string PipeNameDescriptor = "--pipeName";
    internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly NamedPipeClientStream pipe;
    private readonly StreamWriter outWriter;
    private readonly StreamReader inReader;

    public NamedPipeHost(NamedPipeClientStream pipe)
    {
        this.pipe = pipe;
        // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
        outWriter = new(pipe, UTF8NoBOM, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };
        inReader = new(pipe, UTF8NoBOM, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
    }

    public void Dispose()
    {
        outWriter.Dispose();
        inReader.Dispose();
        pipe.Dispose();
    }

    public async ValueTask WriteAsync(string message)
        => await outWriter.WriteAsync(message);

    public async ValueTask WriteLineAsync()
        => await outWriter.WriteLineAsync();

    public async ValueTask WriteLineAsync(string message)
        => await outWriter.WriteLineAsync(message);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
    {
        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            System.Threading.Thread.Sleep(1);
        }

        await WriteLineAsync(Engine.Signals.ToMessage(hostSignal));

        // Read the response from Parent process.
        string? acknowledgment = await inReader.ReadLineAsync();
        if (acknowledgment != Engine.Signals.Acknowledgment
            && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
        {
            throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }
    }

    public async ValueTask SendErrorAsync(string message)
        => await outWriter.WriteLineAsync($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public ValueTask ReportResultsAsync(RunResults runResults)
        => runResults.WriteAsync(this);

    public static async ValueTask<IHost> GetHostAsync(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == PipeNameDescriptor)
            {
                var pipeName = args[i + 1]; // IndexOutOfRangeException means a bug (incomplete data)
                var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await pipe.ConnectAsync();
                return new NamedPipeHost(pipe);
            }
        }
        return new NoAcknowledgementConsoleHost();
    }

    public static NamedPipeServerStream GetPipeServerStream(BenchmarkId benchmarkId, out string pipeName)
    {
        int retryCounter = 0;
        while (true)
        {
            try
            {
                // MacOS has a small character limit, so we use random file name instead of guid.
                pipeName = $"BDN-{benchmarkId.Value}-{Path.GetRandomFileName().Replace(".", "")}";
                return new(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            }
            catch (IOException)
            {
                // Rare case where the pipe name is already in use, retry up to 5 times.
                if (++retryCounter >= 5)
                {
                    throw;
                }
            }
        }
    }
}
