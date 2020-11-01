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

                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "11.0.2").WithId("11.0.2"));
                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "11.0.1").WithId("11.0.1"));
                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "10.0.3").WithId("10.0.3"));
                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "10.0.2").WithId("10.0.2"));
                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "10.0.1").WithId("10.0.1"));
                AddJob(baseJob.WithNuGet("Newtonsoft.Json", "9.0.1").WithId("9.0.1"));
            }
        }

        [Benchmark]
        public void SerializeAnonymousObject()
            => JsonConvert.SerializeObject(
                new { hello = "world", price = 1.99, now = DateTime.UtcNow });
    }
}