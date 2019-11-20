namespace BenchmarkDotNet.Engines
{
    public sealed class NullObjectHost : IHost
    {
        public void ReportResults(RunResults runResults) { }

        public void SendError(string message) { }

        public void SendSignal(HostSignal hostSignal) { }

        public void Write(string message) { }

        public void WriteLine() { }

        public void WriteLine(string message) { }
    }
}
