namespace BenchmarkDotNet
{
    public class BenchmarkState
    {
        public int IntParam { get; set; }

        public int Iteration { get; set; }
        public BenchmarkIterationMode IterationMode { get; set; }
        public long Operation { get; set; }

        // TODO: make it threadlocal for multithreading benchmarks
        public static readonly BenchmarkState Instance = new BenchmarkState();
    }
}