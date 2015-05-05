using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    public class Benchmark
    {
        public BenchmarkTarget Target { get; }
        public BenchmarkTask Task { get; }

        public string Caption => Target.Caption + "_" + Task.Configuration.Caption;
        public string Description => $"{Target.Description} ({Task.Configuration.Caption}) [{Task.Settings.ToArgs()}]";

        public Benchmark(BenchmarkTarget target, BenchmarkTask task)
        {
            Target = target;
            Task = task;
        }
    }
}