using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroPowerPlan
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.MediumRun.WithPowerPlan(new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61")));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.UltimatePerformance));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.UserPowerPlan));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.HighPerformance));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.Balanced));
                Add(Job.MediumRun.WithPowerPlan(PowerPlan.PowerSaver));
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
