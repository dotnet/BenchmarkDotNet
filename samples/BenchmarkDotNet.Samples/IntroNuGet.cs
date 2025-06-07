using System;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    /// <summary>
    /// Benchmarks between various versions of a NuGet package
    /// </summary>
    /// <remarks>
    /// Only supported with the CsProjCoreToolchain toolchain
    /// </remarks>
    [Config(typeof(Config))]
    public class IntroNuGet
    {
        // Specify jobs with different versions of the same NuGet package to benchmark.
        // The NuGet versions referenced on these jobs must be greater or equal to the
        // same NuGet version referenced in this benchmark project.
        // Example: This benchmark project references Newtonsoft.Json 13.0.1
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun;

                string[] targetVersions = [
                    "9.0.3",
                    "9.0.4",
                    "9.0.5",
                ];

                foreach (var version in targetVersions)
                {
                    AddJob(baseJob.WithNuGet("System.Collections.Immutable", version)
                                  .WithId("v"+version));
                }
            }
        }

        private static readonly Random rand = new Random(Seed: 0);
        private static readonly double[] values = Enumerable.Range(1, 10_000).Select(x => rand.NextDouble()).ToArray();

        [Benchmark]
        public void ToImmutableArrayBenchmark()
        {
            var results = values.ToImmutableArray();
        }
    }
}