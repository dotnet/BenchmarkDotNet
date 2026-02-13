using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public class TcpHost : IHost
{
    internal const string TcpPortDescriptor = "--tcpPort";
    internal static readonly Encoding UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromMinutes(1);

    private readonly StreamWriter outWriter;
    private readonly StreamReader inReader;

    public TcpHost(TcpClient client)
    {
        var stream = client.GetStream();
        // Flush the data to the Stream after each write, otherwise the host process will wait for input endlessly!
        outWriter = new(stream, UTF8NoBOM) { AutoFlush = true };
        inReader = new(stream, UTF8NoBOM, detectEncodingFromByteOrderMarks: false);
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
            Thread.Sleep(1);
        }

        await outWriter.WriteLineAsync(Engine.Signals.ToMessage(hostSignal));

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
            if (args[i] == TcpPortDescriptor)
            {
                int port = int.Parse(args[i + 1]);
                var client = new TcpClient();
#if NETSTANDARD2_0
                client.Connect(IPAddress.Loopback, port);
                await client.ConnectAsync(IPAddress.Loopback, port);
#else
                try
                {
                    using var cts = new CancellationTokenSource(ConnectionTimeout);
                    await client.ConnectAsync(IPAddress.Loopback, port, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException($"The connection to the host process timed out after {ConnectionTimeout}.");
                }
#endif
                return new TcpHost(client);
            }
        }
        return new NoAcknowledgementConsoleHost();
    }
}
