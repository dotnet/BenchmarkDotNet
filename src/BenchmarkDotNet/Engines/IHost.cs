using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHost : IDisposable
{
    void WriteLine();
    void WriteLine(string message);

    ValueTask SendSignalAsync(HostSignal hostSignal);

    void SendError(string message);
    void ReportResults(RunResults runResults);
}
