using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess;
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

        public static IEnumerable<object[]> GetToolchains()
            => new[]
            {
                new object[] { Job.Default.GetToolchain() },
                new object[] { InProcessToolchain.Instance }
            };

        public class AccurateAllocations
        {
            [Benchmark] public void Nothing() { }
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];

            [Benchmark] public Task<int> AllocateTask() => Task.FromResult(default(int));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void MemoryDiagnoserIsAccurate(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = objectAllocationOverhead + IntPtr.Size; // + array length

            AssertAllocations(toolchain, typeof(AccurateAllocations), new Dictionary<string, long>
            {
                { nameof(AccurateAllocations.Nothing), 0 },
                { nameof(AccurateAllocations.EightBytesArray), 8 + arraySizeOverhead },
                { nameof(AccurateAllocations.SixtyFourBytesArray), 64 + arraySizeOverhead },

                { nameof(AccurateAllocations.AllocateTask), SizeOfAllFields<Task<int>>() + objectAllocationOverhead },
            });
        }

        public class AllocatingSetupAndCleanup
        {
            private List<int> list;

            [Benchmark] public void AllocateNothing() { }

            [Setup] public void AllocatingSetUp() => AllocateUntilGcWakesUp();
            [Cleanup] public void AllocatingCleanUp() => AllocateUntilGcWakesUp();

            private void AllocateUntilGcWakesUp()
            {
                int initialCollectionCount = GC.CollectionCount(0);

                while (initialCollectionCount == GC.CollectionCount(0))
                    list = Enumerable.Range(0, 100).ToList();
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(AllocatingSetupAndCleanup), new Dictionary<string, long>
            {
                { nameof(AllocatingSetupAndCleanup.AllocateNothing), 0 }
            });
        }

        public class NoAllocationsAtAll
        {
            [Benchmark] public void EmptyMethod() { }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void EngineShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(NoAllocationsAtAll), new Dictionary<string, long>
            {
                { nameof(NoAllocationsAtAll.EmptyMethod), 0 }
            });
        }

        public class NonAllocatingAsynchronousBenchmarks
        {
            private readonly Task<int> completedTaskOfT = Task.FromResult(default(int)); // we store it in the field, because Task<T> is reference type so creating it allocates heap memory

            [Benchmark] public Task CompletedTask() => Task.CompletedTask;

            [Benchmark] public Task<int> CompletedTaskOfT() => completedTaskOfT;

            [Benchmark] public ValueTask<int> CompletedValueTaskOfT() => new ValueTask<int>(default(int));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        public void AwaitingTasksShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(NonAllocatingAsynchronousBenchmarks), new Dictionary<string, long>
            {
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTask), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTaskOfT), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedValueTaskOfT), 0 }
            });
        }

        private void AssertAllocations(IToolchain toolchain, Type benchmarkType, Dictionary<string, long> benchmarksAllocationsValidators)
        {
            var config = CreateConfig(toolchain);
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

        private IConfig CreateConfig(IToolchain toolchain)
        {
            return ManualConfig.CreateEmpty()
                .With(Job.ShortRun.WithGcForce(false).With(toolchain))
                .With(DefaultConfig.Instance.GetLoggers().ToArray())
                .With(DefaultColumnProviders.Instance)
                .With(MemoryDiagnoser.Default)
                .With(new OutputLogger(output));
        }

        // note: don't copy, never use in production systems (it should work but I am not 100% sure)
        private int SizeOfAllFields<T>()
        {
            Func<Type, int> getSize = type =>
            {
                var sizeOf = typeof(Unsafe).GetTypeInfo().GetMethod(nameof(Unsafe.SizeOf));

                return (int)sizeOf.MakeGenericMethod(type).Invoke(null, null);
            };

            return typeof(T)
                .GetAllFields()
                .Where(field => !field.IsStatic && !field.IsLiteral)
                .Distinct()
                .Sum(field => getSize(field.FieldType));
        }
    }
}