using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Snapshot;
using System.Threading;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(Config))]
    public class IntroSnapshot
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                var store = Toolchains.Snapshot.Stores.JsonSnapshotStore.From("./snapshot.js");
                var toolchain = SnapshotToolchain.From(store);
                AddJob(Job.Default.WithToolchain(toolchain).WithBaseline(true).WithId("Baseline"));
                AddJob(Job.ShortRun);
                AddExporter(SnapshotExporter.From(store));
            }
        }
        // And define a method with the Benchmark attribute
        [Benchmark]
        public void Sleep() => Thread.Sleep(10);

        // You can write a description for your method.
        [Benchmark(Description = "Thread.Sleep(15)")]
        public void SleepWithDescription() => Thread.Sleep(15);
    }
}
