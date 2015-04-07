using BenchmarkDotNet.Settings;

namespace BenchmarkDotNet.Samples
{
    public class ExplicitlyBenchmarkCompetition : ISample
    {
        private const int IterationCount = 100000001;

        public void Run()
        {
            var benchmark1 = new Benchmark("Loop1A", () => Loop1A());
            var benchmark2 = new Benchmark("Loop2", () => Loop2());
            var benchmark3 = new Benchmark("Loop3", () => Loop3());
            var benchmark4 = new Benchmark("Loop1B", () => Loop1B());
            var settings = BenchmarkSettings.Build(BenchmarkSettings.DetailedMode.Create(true));
            var runner = new BenchmarkRunner(settings);
            runner.RunCompetition(new[] { benchmark1, benchmark2, benchmark3, benchmark4 });
        }

        public int Loop1A()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }

        public int Loop1B()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount; i++)
                counter++;
            return counter;
        }

        public int Loop2()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount * 2; i++)
                counter++;
            return counter;
        }

        public int Loop3()
        {
            int counter = 0;
            for (int i = 0; i < IterationCount * 3; i++)
                counter++;
            return counter;
        }
    }
}