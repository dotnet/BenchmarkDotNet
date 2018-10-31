using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using Newtonsoft.Json;

namespace BenchmarkDotNet.Samples
{
    /// <summary>
    /// Benchmarks between various versions of a NuGet package
    /// </summary>
    /// <remarks>
    /// Only supported with the DotNetCliBuilder toolchain
    /// </remarks>
    [Config(typeof(Config))]
    public class IntroNuGet
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                //Specify jobs with different versions of the same NuGet package to benchmark.
                //The NuGet versions referenced on these jobs must be greater or equal to the 
                //same NuGet version referenced in this benchmark project.
                //Example: This benchmark project references Newtonsoft.Json 9.0.1
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "11.0.2").WithId("11.0.2"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "11.0.1").WithId("11.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "10.0.3").WithId("10.0.3"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "10.0.2").WithId("10.0.2"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "10.0.1").WithId("10.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuGet("Newtonsoft.Json", "9.0.1").WithId("9.0.1"));
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(CsvExporter.Default);
            }
        }

        [Benchmark]
        public void SerializeAnonymousObject() => JsonConvert.SerializeObject(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
    }
}
