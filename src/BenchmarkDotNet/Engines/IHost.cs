namespace BenchmarkDotNet.Engines
{
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
