using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Engines
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IHost
    {
        void Write(string message);
        void WriteLine();
        void WriteLine(string message);

        void SendSignal(HostSignal hostSignal);
        void SendError(string message);

        void ReportResults(RunResults runResults);
    }
}
