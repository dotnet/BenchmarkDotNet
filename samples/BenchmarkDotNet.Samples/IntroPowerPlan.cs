using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroPowerPlan
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.HighPerformance));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.UserPowerPlan));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.Balanced));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.PowerSaver));
                Add(Job.MediumRun.WithPowerPlan("a1841308-3541-4fab-bc81-f71556f20b4a"));
            }
        }

        [Benchmark]
        public int IterationTest()
        {
            int j = 0;
            for (int i = 0; i < short.MaxValue; ++i)
            {
                j = i;
            }

            return j;
        }

        [Benchmark]
        public int SplitJoin()
            => string.Join(",", new string[1000]).Split(',').Length;
    }
}
