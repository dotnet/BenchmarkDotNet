#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;
using System;
using System.ComponentModel;
using System.Threading;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
[SupportedOSPlatform("browser")]
// Must be public for JSExport, but should not be used directly by users.
public sealed partial class WebSocketHost : IHost
{
    private static TaskCompletionSource<string>? receiveMessageTaskSource;

    [JSExport]
    public static void ReceiveMessage(string message)
    {
        var source = receiveMessageTaskSource;
        receiveMessageTaskSource = null;
        source!.SetResult(message);
    }

    [JSImport("sendToParent", "ipc")]
    static partial void SendMessage(string message);

    public void Dispose() { }

    public void WriteLine()
        => SendMessage("");

    public void WriteLine(string message)
        => SendMessage(message);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
    {
        if (receiveMessageTaskSource is not null)
        {
            throw new InvalidOperationException("WebSocketHost.SendSignalAsync does not support concurrent calls.");
        }
        var source = new TaskCompletionSource<string>();
        receiveMessageTaskSource = source;

        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            Thread.Sleep(1);
        }

        SendMessage(Engine.Signals.ToMessage(hostSignal));

        // Read the response from Parent process.
        string? acknowledgment = await source.Task;
        if (acknowledgment != Engine.Signals.Acknowledgment
            && !(acknowledgment is null && hostSignal == HostSignal.AfterAll)) // an early EOF, but still valid
        {
            throw new NotSupportedException($"Unknown Acknowledgment: {acknowledgment}");
        }
    }

    public void SendError(string message)
        => SendMessage($"{ValidationErrorReporter.ConsoleErrorPrefix} {message}");

    public void ReportResults(RunResults runResults)
        => runResults.Print(this);
}
#endif
