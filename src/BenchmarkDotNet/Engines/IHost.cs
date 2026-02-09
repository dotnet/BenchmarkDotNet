using JetBrains.Annotations;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHost : IDisposable
{
    ValueTask WriteLineAsync();
    ValueTask WriteLineAsync(string message);

    ValueTask SendSignalAsync(HostSignal hostSignal);
    ValueTask SendErrorAsync(string message);

    ValueTask ReportResultsAsync(RunResults runResults);
}
