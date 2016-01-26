namespace BenchmarkDotNet.Running
{
    public class State
    {
        public int Iteration { get; set; }
        public IterationMode IterationMode { get; set; }
        public long Operation { get; set; }

        // TODO: make it threadlocal for multithreading benchmarks
        public static readonly State Instance = new State();
    }
}