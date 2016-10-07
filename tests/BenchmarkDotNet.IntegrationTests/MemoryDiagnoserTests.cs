#if !CORE
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Diagnostics.Windows;

namespace BenchmarkDotNet.IntegrationTests
{
    public class NewVsStackalloc
    {
        [Benchmark]
        public void New() => Blackhole(new byte[100]);

        [Benchmark]
        public unsafe void Stackalloc()
        {
            var bytes = stackalloc byte[100];
            Blackhole(bytes);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Blackhole<T>(T input) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void Blackhole(byte* input) { }
    }

    public class AllocatingSetupAndCleanup
    {
        private List<int> list;

        [Setup]
        public void AllocatingSetUp() => AllocateUntilGcWakesUp();

        [Benchmark]
        public void AllocateNothing() { }

        [Cleanup]
        public void AllocatingCleanUp() => AllocateUntilGcWakesUp();

        private void AllocateUntilGcWakesUp()
        {
            int initialCollectionCount = GC.CollectionCount(0);

            while (initialCollectionCount == GC.CollectionCount(0))
            {
                list = Enumerable.Range(0, 100).ToList();
            }
        }
    }

    [KeepBenchmarkFiles()]
    public class NoAllocationsAtAll
    {
        [Benchmark]
        public void EmptyMethod() { }
    }

    // this class is not compiled for CORE because it is using Diagnosers that currently do not support Core
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        [Fact(Skip = "Temporarily suppressed, see https://github.com/PerfDotNet/BenchmarkDotNet/issues/208")]
        public void MemoryDiagnoserTracksHeapMemoryAllocation()
        {
            var memoryDiagnoser = new MemoryDiagnoser();
            var config = CreateConfig(memoryDiagnoser, 50);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(typeof(NewVsStackalloc), config);

            var summary = BenchmarkRunner.Run((Benchmark[])benchmarks, config);

            var gcCollectionColumns = GetColumns<MemoryDiagnoser.GCCollectionColumn>(memoryDiagnoser).ToArray();
            var stackallocBenchmarks = benchmarks.Where(benchmark => benchmark.DisplayInfo.Contains("Stackalloc"));
            var newArrayBenchmarks = benchmarks.Where(benchmark => benchmark.DisplayInfo.Contains("New"));

            const int gen0Index = 0;

            foreach (var benchmark in stackallocBenchmarks)
            {
                var gen0Collections = gcCollectionColumns[gen0Index].GetValue(summary, benchmark);

                Assert.Equal("-", gen0Collections);
            }

            foreach (var benchmark in newArrayBenchmarks)
            {
                var gen0Str = gcCollectionColumns[gen0Index].GetValue(summary, benchmark);

                AssertParsed(gen0Str, gen0Value => gen0Value > 0);
            }
        }

        [Fact(Skip = "Temporarily suppressed, see https://github.com/PerfDotNet/BenchmarkDotNet/issues/208")]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup()
        {
            AssertZeroAllocations(typeof(AllocatingSetupAndCleanup), "AllocateNothing", targetCount: 50);
        }

        [Fact(Skip = "Temporarily suppressed, see https://github.com/PerfDotNet/BenchmarkDotNet/issues/208")]
        public void EngineShouldNotInterfereAllocationResults()
        {
            AssertZeroAllocations(typeof(NoAllocationsAtAll), "EmptyMethod", targetCount: 5000); // we need a lot of iterations to be sure!!
        }

        public void AssertZeroAllocations(Type benchmarkType, string benchmarkMethodName, int targetCount)
        {
            var memoryDiagnoser = new MemoryDiagnoser();
            var config = CreateConfig(memoryDiagnoser, targetCount);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run((Benchmark[])benchmarks, config);

            var allocationColumn = GetColumns<MemoryDiagnoser.AllocationColumn>(memoryDiagnoser).Single();
            var allocateNothingBenchmarks = benchmarks.Where(benchmark => benchmark.DisplayInfo.Contains(benchmarkMethodName));

            foreach (var benchmark in allocateNothingBenchmarks)
            {
                var allocations = allocationColumn.GetValue(summary, benchmark);

                AssertParsed(allocations, allocatedBytes => allocatedBytes == 0);
            }
        }

        private IConfig CreateConfig(IDiagnoser diagnoser, int targetCount)
        {
            return ManualConfig.CreateEmpty()
                               .With(Job.Dry.WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(targetCount).With(GcMode.Default.WithForce(false)))
                               .With(DefaultConfig.Instance.GetLoggers().ToArray())
                               .With(DefaultColumnProviders.Instance)
                               .With(diagnoser)
                               .With(new OutputLogger(output));
        }

        private static T[] GetColumns<T>(MemoryDiagnoser memoryDiagnoser)
            => memoryDiagnoser.GetColumnProvider().GetColumns(null).OfType<T>().ToArray();

        private static void AssertParsed(string text, Predicate<double> condition)
        {
            double value;
            if (double.TryParse(text, NumberStyles.Number, HostEnvironmentInfo.MainCultureInfo, out value))
            {
                Assert.True(condition(value));
            }
            else
            {
                Assert.True(false, $"Can't parse '{text}'");
            }
        }
    }
}
#endif