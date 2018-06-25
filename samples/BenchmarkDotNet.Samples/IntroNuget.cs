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

namespace BenchmarkDotNet.Samples

    [Config(typeof(Config))]
    public class IntroNuget
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                //TODO: So far only implemented with any toolchain using DotNetCliBuilder
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "11.0.1").WithId("11.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "10.0.3").WithId("10.0.3"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "9.0.1").WithId("9.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "8.0.1").WithId("8.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "7.0.1").WithId("7.0.1"));
                Add(Job.MediumRun.With(CsProjCoreToolchain.Current.Value).WithNuget("Newtonsoft.Json", "6.0.1").WithId("6.0.1"));
                Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(CsvExporter.Default);
            }
        }

        [GlobalSetup]
        public void Setup()
        {
            var jsonConvertMethod = Assembly.Load("Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert")
                .GetMethods().Where(x => x.Name == "SerializeObject" && x.GetParameters().Length == 1)
                .First();

            Serializer = s => (string)jsonConvertMethod.Invoke(null, new object[] { s });
        }

        public Func<object, string> Serializer { get; private set; }

        [Benchmark]
        public void SerializeAnonymousObject() => Serializer(new { hello = "world", price = 1.99, now = DateTime.UtcNow });
    }
}
