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

namespace BenchmarkDotNet.IntegrationTests
{
    public class MemoryDiagnoserTests
    {
        private const string SkipAllocationsTests
#if CORE
         = "Not supported for .NET Core yet";
#else
         = null;
#endif

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
            [Benchmark]public byte[] ThousandBytes() => new byte[1000];
        }

        [Fact(Skip = SkipAllocationsTests)]
        public void MemoryDiagnoserIsAccurate()
        {
            double objectAllocationOverhead = IntPtr.Size * 3; // pointer to method table + object header word + pointer to the object 
            AssertAllocations(typeof(AccurateAllocations), 100, new Dictionary<string, Predicate<double>>
            {
                { "Empty", allocatedBytes => allocatedBytes == 0 },
                { "EightBytes", allocatedBytes => allocatedBytes == 8 + objectAllocationOverhead },
                { "SixtyFourBytes", allocatedBytes => allocatedBytes == 64 + objectAllocationOverhead },
                { "ThousandBytes", allocatedBytes => allocatedBytes == 1000 + objectAllocationOverhead }
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

        [Fact(Skip = SkipAllocationsTests)]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup()
        {
            AssertAllocations(typeof(AllocatingSetupAndCleanup), 5, new Dictionary<string, Predicate<double>>
            {
                { "AllocateNothing",  allocatedBytes => allocatedBytes == 0 }
            });
        }

        public class NoAllocationsAtAll
        {
            [Benchmark]public void EmptyMethod() { }
        }

        [Fact(Skip = SkipAllocationsTests)]
        public void EngineShouldNotInterfereAllocationResults()
        {
            AssertAllocations(typeof(NoAllocationsAtAll), 100, new Dictionary<string, Predicate<double>>
            {
                { "EmptyMethod",  allocatedBytes => allocatedBytes == 0 }
            });
        }

        private void AssertAllocations(Type benchmarkType, int targetCount,
            Dictionary<string, Predicate<double>> benchmarksAllocationsValidators)
        {
            var memoryDiagnoser = MemoryDiagnoser.Default;
            var config = CreateConfig(memoryDiagnoser, targetCount);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run((Benchmark[])benchmarks, config);

            var allocationColumn = GetColumns<MemoryDiagnoser.AllocationColumn>(memoryDiagnoser).Single();

            foreach (var benchmarkAllocationsValidator in benchmarksAllocationsValidators)
            {
                var allocatingBenchmarks = benchmarks.Where(benchmark => benchmark.DisplayInfo.Contains(benchmarkAllocationsValidator.Key));

                foreach (var benchmark in allocatingBenchmarks)
                {
                    var allocations = allocationColumn.GetValue(summary, benchmark);

                    AssertParsed(allocations, benchmarkAllocationsValidator.Value);
                }
            }
        }

        private IConfig CreateConfig(IDiagnoser diagnoser, int targetCount)
        {
            return ManualConfig.CreateEmpty()
                .With(
                    Job.Dry
                        .WithLaunchCount(1)
                        .WithWarmupCount(1)
                        .WithTargetCount(targetCount)
                        .WithInvocationCount(100)
                        .WithGcForce(false))
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
                Assert.True(condition(value), $"Failed for value {value}");
            }
            else
            {
                Assert.True(false, $"Can't parse '{text}'");
            }
        }
    }
}