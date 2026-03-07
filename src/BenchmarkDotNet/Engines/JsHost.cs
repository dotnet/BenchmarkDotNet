#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Validators;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[AggressivelyOptimizeMethods]
[EditorBrowsable(EditorBrowsableState.Never)]
[SupportedOSPlatform("browser")]
// Must be public for JSExport, but should not be used directly by users.
public sealed partial class JsHost : IHost
{
    private static TaskCompletionSource<string>? signalAckTaskSource;

    [JSImport("sendToParent", "ipc")]
    static partial void SendMessage(string message);

    [JSImport("sendSignalToParent", "ipc")]
    static partial void SendSignal(string message);

    [JSExport]
    public static void OnSignalAcknowledged(string acknowledgment)
    {
        var source = signalAckTaskSource;
        signalAckTaskSource = null;
        source?.SetResult(acknowledgment);
    }

    public void Dispose() { }

    public void WriteLine()
        => SendMessage("");

    public void WriteLine(string message)
        => SendMessage(message);

    public async ValueTask SendSignalAsync(HostSignal hostSignal)
    {
        if (signalAckTaskSource is not null)
        {
            throw new InvalidOperationException("JsHost.SendSignalAsync does not support concurrent calls.");
        }

        var source = new TaskCompletionSource<string>();
        signalAckTaskSource = source;

        if (hostSignal == HostSignal.AfterAll)
        {
            // Before the last signal is reported and the benchmark process exits,
            // add an artificial sleep to increase the chance of host process reading all std output.
            await Task.Delay(10);
        }

        SendSignal(Engine.Signals.ToMessage(hostSignal));

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
