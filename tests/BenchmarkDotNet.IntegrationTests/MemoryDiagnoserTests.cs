using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.CoreRt;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper) => output = outputHelper;

        public static IEnumerable<object[]> GetToolchains()
            => RuntimeInformation.IsMono // https://github.com/mono/mono/issues/8397
                ? Array.Empty<object[]>()
                : new[]
                {
                    new object[] { Job.Default.GetToolchain() },
                    new object[] { InProcessEmitToolchain.Instance },
#if NETCOREAPP2_1
                    // we don't want to test CoreRT twice (for .NET 4.6 and Core 2.1) when running the integration tests (these tests take a lot of time)
                    // we test against specific version to keep this test stable
                    new object[] { CoreRtToolchain.CreateBuilder().UseCoreRtNuGet(microsoftDotNetILCompilerVersion: "1.0.0-alpha-27408-02").ToToolchain() }
#endif
                };

        public class AccurateAllocations
        {
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];

            [Benchmark] public Task<int> AllocateTask() => Task.FromResult(default(int));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserIsAccurate(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length

            AssertAllocations(toolchain, typeof(AccurateAllocations), new Dictionary<string, long>
            {
                { nameof(AccurateAllocations.EightBytesArray), 8 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateAllocations.SixtyFourBytesArray), 64 + objectAllocationOverhead + arraySizeOverhead },

                { nameof(AccurateAllocations.AllocateTask), CalculateRequiredSpace<Task<int>>() },
            });
        }

        public class AccurateSurvived
        {
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];
            [Benchmark] public Task<int> AllocateTask() => Task.FromResult(default(int));


            public byte[] bytes8;
            public byte[] bytes64;
            public Task<int> task;

            [GlobalSetup(Targets = new string[] { nameof(EightBytesArrayNoAllocate), nameof(SixtyFourBytesArrayNoAllocate) })]
            public void SetupNoAllocate()
            {
                bytes8 = new byte[8];
                bytes64 = new byte[64];
            }

            [Benchmark] public byte[] EightBytesArrayNoAllocate() => bytes8;
            [Benchmark] public byte[] SixtyFourBytesArrayNoAllocate() => bytes64;


            [Benchmark] public void EightBytesArraySurvive() => bytes8 = new byte[8];
            [Benchmark] public void SixtyFourBytesArraySurvive() => bytes64 = new byte[64];
            [Benchmark] public void AllocateTaskSurvive() => task = Task.FromResult(default(int));


            [Benchmark] public void EightBytesArrayAllocateNoSurvive() => DeadCodeEliminationHelper.KeepAliveWithoutBoxing(new byte[8]);
            [Benchmark] public void SixtyFourBytesArrayAllocateNoSurvive() => DeadCodeEliminationHelper.KeepAliveWithoutBoxing(new byte[64]);
            [Benchmark] public void TaskAllocateNoSurvive() => DeadCodeEliminationHelper.KeepAliveWithoutBoxing(Task.FromResult(default(int)));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserSurvivedIsAccurate(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length

            AssertSurvived(toolchain, typeof(AccurateSurvived), new Dictionary<string, long>
            {
                { nameof(AccurateSurvived.EightBytesArray), 0 },
                { nameof(AccurateSurvived.SixtyFourBytesArray), 0 },
                { nameof(AccurateSurvived.AllocateTask), 0 },

                { nameof(AccurateSurvived.EightBytesArrayNoAllocate), 0 },
                { nameof(AccurateSurvived.SixtyFourBytesArrayNoAllocate), 0 },

                { nameof(AccurateSurvived.EightBytesArraySurvive), 8 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateSurvived.SixtyFourBytesArraySurvive), 64 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateSurvived.AllocateTaskSurvive), CalculateRequiredSpace<Task<int>>() },

                { nameof(AccurateSurvived.EightBytesArrayAllocateNoSurvive), 0 },
                { nameof(AccurateSurvived.SixtyFourBytesArrayAllocateNoSurvive), 0 },
                { nameof(AccurateSurvived.TaskAllocateNoSurvive), 0 },
            });
        }

        public class AllocatingGlobalSetupAndCleanup
        {
            private List<int> list;

            [Benchmark] public void AllocateNothing() { }

            [IterationSetup]
            [GlobalSetup]
            public void AllocatingSetUp() => AllocateUntilGcWakesUp();

            [IterationCleanup]
            [GlobalCleanup]
            public void AllocatingCleanUp() => AllocateUntilGcWakesUp();

            private void AllocateUntilGcWakesUp()
            {
                int initialCollectionCount = GC.CollectionCount(0);

                while (initialCollectionCount == GC.CollectionCount(0))
                    list = Enumerable.Range(0, 100).ToList();
            }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(AllocatingGlobalSetupAndCleanup), new Dictionary<string, long>
            {
                { nameof(AllocatingGlobalSetupAndCleanup.AllocateNothing), 0 }
            });
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserDoesNotIncludeSurvivedFromSetupAndCleanup(IToolchain toolchain)
        {
            AssertSurvived(toolchain, typeof(AllocatingGlobalSetupAndCleanup), new Dictionary<string, long>
            {
                { nameof(AllocatingGlobalSetupAndCleanup.AllocateNothing), 0 }
            });
        }

        public class NoAllocationsAtAll
        {
            [Benchmark] public void EmptyMethod() { }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void EngineShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(NoAllocationsAtAll), new Dictionary<string, long>
            {
                { nameof(NoAllocationsAtAll.EmptyMethod), 0 }
            });
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void EngineShouldNotInterfereSurvivedResults(IToolchain toolchain)
        {
            AssertSurvived(toolchain, typeof(NoAllocationsAtAll), new Dictionary<string, long>
            {
                { nameof(NoAllocationsAtAll.EmptyMethod), 0 }
            });
        }

        public class NoBoxing
        {
            [Benchmark] public ValueTuple<int> ReturnsValueType() => new ValueTuple<int>(0);
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void EngineShouldNotIntroduceBoxing(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(NoBoxing), new Dictionary<string, long>
            {
                { nameof(NoBoxing.ReturnsValueType), 0 }
            });
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void EngineShouldNotIntroduceBoxingSurvived(IToolchain toolchain)
        {
            AssertSurvived(toolchain, typeof(NoBoxing), new Dictionary<string, long>
            {
                { nameof(NoBoxing.ReturnsValueType), 0 }
            });
        }

        public class NonAllocatingAsynchronousBenchmarks
        {
            private readonly Task<int> completedTaskOfT = Task.FromResult(default(int)); // we store it in the field, because Task<T> is reference type so creating it allocates heap memory

            [GlobalSetup]
            public void Setup()
            {
                // Run once to set static memory.
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(CompletedTask());
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(CompletedTaskOfT());
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(CompletedValueTaskOfT());
            }

            [Benchmark] public Task CompletedTask() => Task.CompletedTask;

            [Benchmark] public Task<int> CompletedTaskOfT() => completedTaskOfT;

            [Benchmark] public ValueTask<int> CompletedValueTaskOfT() => new ValueTask<int>(default(int));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AwaitingTasksShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(NonAllocatingAsynchronousBenchmarks), new Dictionary<string, long>
            {
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTask), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTaskOfT), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedValueTaskOfT), 0 }
            });
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AwaitingTasksShouldNotInterfereSurvivedResults(IToolchain toolchain)
        {
            AssertSurvived(toolchain, typeof(NonAllocatingAsynchronousBenchmarks), new Dictionary<string, long>
            {
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTask), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedTaskOfT), 0 },
                { nameof(NonAllocatingAsynchronousBenchmarks.CompletedValueTaskOfT), 0 }
            });
        }

        public class WithOperationsPerInvokeBenchmarks
        {
            [Benchmark(OperationsPerInvoke = 4)]
            public void WithOperationsPerInvoke()
            {
                DoNotInline(new object(), new object());
                DoNotInline(new object(), new object());
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void DoNotInline(object left, object right) { }
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AllocatedMemoryShouldBeScaledForOperationsPerInvoke(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word

            AssertAllocations(toolchain, typeof(WithOperationsPerInvokeBenchmarks), new Dictionary<string, long>
            {
                { nameof(WithOperationsPerInvokeBenchmarks.WithOperationsPerInvoke), objectAllocationOverhead + IntPtr.Size }
            });
        }

        public class TimeConsuming
        {
            [Benchmark]
            public byte[] SixtyFourBytesArray()
            {
                // this benchmark should hit allocation quantum problem
                // it allocates a little of memory, but it takes a lot of time to execute so we can't run in thousands of times!

                Thread.Sleep(TimeSpan.FromSeconds(0.5));

                return new byte[64];
            }
        }

        [TheoryNetCoreOnly("Only .NET Core 2.0+ API is bug free for this case"), MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AllocationQuantumIsNotAnIssueForNetCore21Plus(IToolchain toolchain)
        {
            if (toolchain is CoreRtToolchain) // the fix has not yet been backported to CoreRT
                return;

            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length

            AssertAllocations(toolchain, typeof(TimeConsuming), new Dictionary<string, long>
            {
                { nameof(TimeConsuming.SixtyFourBytesArray), 64 + objectAllocationOverhead + arraySizeOverhead }
            });
        }

        public class MultiThreadedAllocation
        {
            public const int Size = 1_000_000;
            public const int ThreadsCount = 10;

            private Thread[] threads;

            [IterationSetup]
            public void SetupIteration()
            {
                threads = Enumerable.Range(0, ThreadsCount)
                    .Select(_ => new Thread(() => GC.KeepAlive(new byte[Size])))
                    .ToArray();
            }

            [Benchmark]
            public void Allocate()
            {
                foreach (var thread in threads)
                {
                    thread.Start();
                    thread.Join();
                }
            }
        }

        [TheoryNetCore30(".NET Core 3.0 preview6+ exposes a GC.GetTotalAllocatedBytes method which makes it possible to work"), MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserIsAccurateForMultiThreadedBenchmarks(IToolchain toolchain)
        {
            if (toolchain is CoreRtToolchain) // the API has not been yet ported to CoreRT
                return;

            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length
            long memoryAllocatedPerArray = (MultiThreadedAllocation.Size + objectAllocationOverhead + arraySizeOverhead);
            long threadStartAndJoinOverhead = 112; // this is more or less a magic number taken from memory profiler
            long allocatedMemoryPerThread = memoryAllocatedPerArray + threadStartAndJoinOverhead;

            AssertAllocations(toolchain, typeof(MultiThreadedAllocation), new Dictionary<string, long>
            {
                { nameof(MultiThreadedAllocation.Allocate), allocatedMemoryPerThread * MultiThreadedAllocation.ThreadsCount }
            });
        }

        private void AssertAllocations(IToolchain toolchain, Type benchmarkType, Dictionary<string, long> benchmarksAllocationsValidators)
        {
            var config = CreateConfig(toolchain, MemoryDiagnoser.Default);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run(benchmarks);

            foreach (var benchmarkAllocationsValidator in benchmarksAllocationsValidators)
            {
                // CoreRT is missing some of the CoreCLR threading/task related perf improvements, so sizeof(Task<int>) calculated for CoreCLR < sizeof(Task<int>) on CoreRT
                // see https://github.com/dotnet/corert/issues/5705 for more
                if (benchmarkAllocationsValidator.Key == nameof(AccurateAllocations.AllocateTask) && toolchain is CoreRtToolchain)
                    continue;

                var allocatingBenchmarks = benchmarks.BenchmarksCases.Where(benchmark => benchmark.DisplayInfo.Contains(benchmarkAllocationsValidator.Key));

                foreach (var benchmark in allocatingBenchmarks)
                {
                    var benchmarkReport = summary.Reports.Single(report => report.BenchmarkCase == benchmark);

                    Assert.Equal(benchmarkAllocationsValidator.Value, benchmarkReport.GcStats.BytesAllocatedPerOperation);

                    if (benchmarkAllocationsValidator.Value == 0)
                    {
                        Assert.Equal(0, benchmarkReport.GcStats.GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects: true));
                    }
                }
            }
        }

        private void AssertSurvived(IToolchain toolchain, Type benchmarkType, Dictionary<string, long> benchmarkSurvivedValidators)
        {
            // Core has survived memory measurement problems.
            // See https://github.com/dotnet/runtime/issues/45446
            if (toolchain is CsProjCoreToolchain
                || (toolchain.IsInProcess && RuntimeInformation.IsNetCore)
                || toolchain is CoreRtToolchain) // CoreRt actually does measure accurately in a normal benchmark run, but doesn't with the specific version used in these tests.
                return;

            var config = CreateConfig(toolchain, MemoryDiagnoser.WithSurvived);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run(benchmarks);

            foreach (var benchmarkSurvivedValidator in benchmarkSurvivedValidators)
            {
                var survivedBenchmarks = benchmarks.BenchmarksCases.Where(benchmark => benchmark.Descriptor.WorkloadMethodDisplayInfo == benchmarkSurvivedValidator.Key).ToArray();

                foreach (var benchmark in survivedBenchmarks)
                {
                    var benchmarkReport = summary.Reports.Single(report => report.BenchmarkCase == benchmark);

                    Assert.Equal(benchmarkSurvivedValidator.Value, benchmarkReport.GcStats.SurvivedBytes);
                }
            }
        }

        private IConfig CreateConfig(IToolchain toolchain, MemoryDiagnoser memoryDiagnoser)
            => ManualConfig.CreateEmpty()
                .AddJob(Job.ShortRun
                    .WithEvaluateOverhead(false) // no need to run idle for this test
                    .WithWarmupCount(0) // don't run warmup to save some time for our CI runs
                    .WithIterationCount(1) // single iteration is enough for us
                    .WithGcForce(false)
                    .WithToolchain(toolchain))
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(memoryDiagnoser)
                .AddLogger(toolchain.IsInProcess ? ConsoleLogger.Default : new OutputLogger(output)); // we can't use OutputLogger for the InProcess toolchains because it allocates memory on the same thread

        // note: don't copy, never use in production systems (it should work but I am not 100% sure)
        private int CalculateRequiredSpace<T>()
        {
            int total = SizeOfAllFields<T>();

            if (!typeof(T).GetTypeInfo().IsValueType)
                total += IntPtr.Size * 2; // pointer to method table + object header word

            if (total % IntPtr.Size != 0) // aligning..
                total += IntPtr.Size - (total % IntPtr.Size);

            return total;
        }

        // note: don't copy, never use in production systems (it should work but I am not 100% sure)
        private int SizeOfAllFields<T>()
        {
            int GetSize(Type type)
            {
                var sizeOf = typeof(Unsafe).GetTypeInfo().GetMethod(nameof(Unsafe.SizeOf));

                return (int)sizeOf.MakeGenericMethod(type).Invoke(null, null);
            }

            return typeof(T)
                .GetAllFields()
                .Where(field => !field.IsStatic && !field.IsLiteral)
                .Distinct()
                .Sum(field => GetSize(field.FieldType));
        }
    }
}