using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Newtonsoft.Json;

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
        // Example: This benchmark project references Newtonsoft.Json 9.0.1
        private class Config : ManualConfig
        {
            public Config()
            {
                var baseJob = Job.MediumRun;

                string[] targetVersions = [
                    "13.0.3",
                    "13.0.2",
                    "13.0.1"
                ];

                foreach (var version in targetVersions)
                {
                    AddJob(baseJob.WithNuGet("Newtonsoft.Json", version)
                                  .WithCustomBuildConfiguration(version)
                                  .WithId(version));
                }
            }
        }

        [Benchmark]
        public void SerializeAnonymousObject()
            => JsonConvert.SerializeObject(
                new { hello = "world", price = 1.99, now = DateTime.UtcNow });
    }
}