using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnostics.dotTrace;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DotTraceTests : BenchmarkTestExecutor
    {
        public DotTraceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void DotTraceSmokeTest()
        {
            if (!RuntimeInformation.IsWindows() && RuntimeInformation.IsMono)
            {
                Output.WriteLine("Skip Mono on non-Windows");
                return;
            }

            var config = new ManualConfig().AddJob(
                Job.Dry.WithId("ExternalProcess"),
                Job.Dry.WithToolchain(InProcessEmitToolchain.Instance).WithId("InProcess")
            );
            string snapshotDirectory = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "snapshots");
            if (Directory.Exists(snapshotDirectory))
                Directory.Delete(snapshotDirectory, true);

            CanExecute<Benchmarks>(config);

            Output.WriteLine("---------------------------------------------");
            Output.WriteLine("SnapshotDirectory:" + snapshotDirectory);
            var snapshots = Directory.EnumerateFiles(snapshotDirectory)
                .Where(filePath => Path.GetExtension(filePath).Equals(".dtp", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OrderBy(fileName => fileName)
                .ToList();
            Output.WriteLine("Snapshots:");
            foreach (string snapshot in snapshots)
                Output.WriteLine("* " + snapshot);
            Assert.Equal(2, snapshots.Count);
        }

        [DotTraceDiagnoser]
        public class Benchmarks
        {
            [Benchmark]
            public int Foo()
            {
                var list = new List<object>();
                for (int i = 0; i < 1000000; i++)
                    list.Add(new object());
                return list.Count;
            }
        }
    }
}