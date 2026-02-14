using JetBrains.Annotations;
using System;
using System.ComponentModel;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHost : IDisposable
{
    void WriteLine();
    void WriteLine(string message);

    void SendSignal(HostSignal hostSignal);
    void SendError(string message);

    void ReportResults(RunResults runResults);
}
