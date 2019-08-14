namespace BenchmarkDotNet.Diagnosers
{
    public class ThreadingDiagnoser : IDiagnoser
    {
        public static readonly ThreadingDiagnoser Default = new ThreadingDiagnoser();

        private ThreadingDiagnoser() { }
    }
}