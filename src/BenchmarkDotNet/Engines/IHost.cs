using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHost : IDisposable
{
    CancellationToken CancellationToken { get; }
    void WriteLine();
    void WriteLine(string message);
    void SendError(string message);
    void ReportResults(RunResults runResults);
    ValueTask SendSignalAsync(HostSignal hostSignal);
    ValueTask Yield();
}
