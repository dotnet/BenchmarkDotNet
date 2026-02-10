using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public class NamedPipesHost : IHost
{
    internal const string PipeNamesDescriptor = "--pipeNames";
    internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    public static readonly TimeSpan PipeConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly StreamWriter outWriter;
    private readonly StreamReader inReader;

    public NamedPipesHost(NamedPipeClientStream fromBenchmarkPipe, NamedPipeClientStream toBenchmarkPipe)
    {
        // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
        outWriter = new(fromBenchmarkPipe, UTF8NoBOM) { AutoFlush = true };
        inReader = new(toBenchmarkPipe, UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
    }

    public void Dispose()
    {
        outWriter.Dispose();
        inReader.Dispose();
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
            if (args[i] == PipeNamesDescriptor)
            {
                // IndexOutOfRangeException means a bug (incomplete data)
                var fromBenchmarkPipeName = args[i + 1];
                var toBenchmarkPipeName = args[i + 2];
                var fromBenchmarkPipe = new NamedPipeClientStream(".", fromBenchmarkPipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                var toBenchmarkPipe = new NamedPipeClientStream(".", toBenchmarkPipeName, PipeDirection.In, PipeOptions.Asynchronous);
                await Task.WhenAll([
                    fromBenchmarkPipe.ConnectAsync((int) PipeConnectionTimeout.TotalMilliseconds),
                    toBenchmarkPipe.ConnectAsync((int) PipeConnectionTimeout.TotalMilliseconds)
                ]);
                return new NamedPipesHost(fromBenchmarkPipe, toBenchmarkPipe);
            }
        }
        return new NoAcknowledgementConsoleHost();
    }

    public static NamedPipeServerStream GetPipeServerStream(BenchmarkId benchmarkId, PipeDirection pipeDirection, out string pipeName)
    {
        int retryCounter = 0;
        while (true)
        {
            try
            {
                // MacOS has a small character limit, so we use random file name instead of guid.
                pipeName = $"BDN-{benchmarkId.Value}-{pipeDirection}-{Path.GetRandomFileName().Replace(".", "")}";
                var pipe = new NamedPipeServerStream(pipeName, pipeDirection, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                // Ensure the pipe handle is not inherited to prevent hangs on Windows.
                if (OsDetector.IsWindows())
                {
                    SetHandleInformation(pipe.SafePipeHandle.DangerousGetHandle(), HANDLE_FLAG_INHERIT, 0);
                }
                return pipe;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);

    const int HANDLE_FLAG_INHERIT = 1;
}
