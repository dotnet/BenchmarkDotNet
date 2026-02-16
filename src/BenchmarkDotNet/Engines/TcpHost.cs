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

    public void WriteLine()
    {
        Console.Error.WriteLine($"[TcpHost] Before WriteLine");
        outWriter.WriteLine();
        Console.Error.WriteLine($"[TcpHost] After WriteLine");
    }

    public void WriteLine(string message)
    {
        Console.Error.WriteLine($"[TcpHost] Before WriteLine: {message}");
        outWriter.WriteLine(message);
        Console.Error.WriteLine($"[TcpHost] After WriteLine: {message}");
    }

    public void SendSignal(HostSignal hostSignal)
    {
        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            Thread.Sleep(1);
        }

        Console.Error.WriteLine($"[TcpHost] Before SendSignal: {hostSignal}");
        outWriter.WriteLine(Engine.Signals.ToMessage(hostSignal));
        Console.Error.WriteLine($"[TcpHost] After SendSignal: {hostSignal}");

        // Read the response from Parent process.
        string? acknowledgment = inReader.ReadLine();
        Console.Error.WriteLine($"[TcpHost] After acknowledgment: {acknowledgment}");

        if (acknowledgment != Engine.Signals.Acknowledgment
            && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
        {
            throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }
    }

    public void SendError(string message)
    {
        Console.Error.WriteLine($"[TcpHost] Before SendError: {message}");
        outWriter.WriteLine($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");
        Console.Error.WriteLine($"[TcpHost] After SendError: {message}");
    }

    public void ReportResults(RunResults runResults)
        => runResults.Print(outWriter);

    public static IHost GetHost(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == TcpPortDescriptor)
            {
                int port = int.Parse(args[i + 1]);
                var client = new TcpClient();
                client.Connect(IPAddress.Loopback, port);
                return new TcpHost(client);
            }
        }
        return new NoAcknowledgementConsoleHost();
    }
}
