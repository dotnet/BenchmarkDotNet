namespace BenchmarkDotNet.Engines
{
    public interface IHost
    {
        bool IsDiagnoserAttached { get; }

        void Write(string message);
        void WriteLine();
        void WriteLine(string message);

        void SendSignal(HostSignal hostSignal);

        void ReportResults(RunResults runResults);
    }
}
