using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        public class AccurateAllocations
        {
            [Benchmark]public void Empty() { }
            [Benchmark]public byte[] EightBytes() => new byte[8];
            [Benchmark]public byte[] SixtyFourBytes() => new byte[64];
        }

        [Fact]
        public void MemoryDiagnoserIsAccurate()
        {
            long objectAllocationOverhead = IntPtr.Size * 3; // pointer to method table + object header word + array length
            AssertAllocations(typeof(AccurateAllocations), 200, new Dictionary<string, long>
            {
                { "Empty", 0 },
                { "EightBytes", 8 + objectAllocationOverhead },
                { "SixtyFourBytes", 64 + objectAllocationOverhead },
            });
        }

        public class AllocatingSetupAndCleanup
        {
            private List<int> list;

            [Benchmark]public void AllocateNothing() { }

            [Setup]public void AllocatingSetUp() => AllocateUntilGcWakesUp();
            [Cleanup]public void AllocatingCleanUp() => AllocateUntilGcWakesUp();

            private void AllocateUntilGcWakesUp()
            {
                int initialCollectionCount = GC.CollectionCount(0);

                while (initialCollectionCount == GC.CollectionCount(0))
                    list = Enumerable.Range(0, 100).ToList();
            }
        }

        [Fact]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup()
        {
            AssertAllocations(typeof(AllocatingSetupAndCleanup), 100, new Dictionary<string, long>
            {
                { "AllocateNothing", 0 }
            });
        }

        public class NoAllocationsAtAll
        {
            [Benchmark]public void EmptyMethod() { }
        }

        [Fact]
        public void EngineShouldNotInterfereAllocationResults()
        {
            AssertAllocations(typeof(NoAllocationsAtAll), 100, new Dictionary<string, long>
            {
                { "EmptyMethod", 0 }
            });
        }

        private void AssertAllocations(Type benchmarkType, int targetCount,
            Dictionary<string, long> benchmarksAllocationsValidators)
        {
            var memoryDiagnoser = MemoryDiagnoser.Default;
            var config = CreateConfig(memoryDiagnoser);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run(benchmarks, config);

            foreach (var benchmarkAllocationsValidator in benchmarksAllocationsValidators)
            {
                var allocatingBenchmarks = benchmarks.Where(benchmark => benchmark.DisplayInfo.Contains(benchmarkAllocationsValidator.Key));

                foreach (var benchmark in allocatingBenchmarks)
                {
                    var benchmarkReport = summary.Reports.Single(report => report.Benchmark == benchmark);

                    Assert.Equal(benchmarkAllocationsValidator.Value, benchmarkReport.GcStats.BytesAllocatedPerOperation);
                }
            }
        }

        private IConfig CreateConfig(IDiagnoser diagnoser)
        {
            return ManualConfig.CreateEmpty()
                .With(Job.ShortRun.WithGcForce(false))
                .With(DefaultConfig.Instance.GetLoggers().ToArray())
                .With(DefaultColumnProviders.Instance)
                .With(diagnoser)
                .With(new OutputLogger(output));
        }

        private static T[] GetColumns<T>(MemoryDiagnoser memoryDiagnoser, Summary summary)
            => memoryDiagnoser.GetColumnProvider().GetColumns(summary).OfType<T>().ToArray();
    }
}