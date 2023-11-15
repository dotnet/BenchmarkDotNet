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
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Xunit;
using Xunit.Abstractions;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper) => output = outputHelper;

        public static IEnumerable<object[]> GetToolchains()
        {
            if (RuntimeInformation.IsOldMono) // https://github.com/mono/mono/issues/8397
                yield break;

            yield return new object[] { Job.Default.GetToolchain() };
            yield return new object[] { InProcessEmitToolchain.Instance };
        }

        public class AccurateAllocations
        {
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];

            [Benchmark] public Task<int> AllocateTask() => Task.FromResult<int>(-12345);
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserIsAccurate(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length

            if (toolchain is MonoToolchain)
            {
                objectAllocationOverhead += IntPtr.Size;
            }

            AssertAllocations(toolchain, typeof(AccurateAllocations), new Dictionary<string, long>
            {
                { nameof(AccurateAllocations.EightBytesArray), 8 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateAllocations.SixtyFourBytesArray), 64 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateAllocations.AllocateTask), CalculateRequiredSpace<Task<int>>() },
            });
        }

        [FactEnvSpecific("We don't want to test NativeAOT twice (for .NET Framework 4.6.2 and .NET 7.0)", EnvRequirement.DotNetCoreOnly)]
        public void MemoryDiagnoserSupportsNativeAOT()
        {
            if (RuntimeInformation.IsMacOS())
                return; // currently not supported

            MemoryDiagnoserIsAccurate(
                NativeAotToolchain.CreateBuilder()
                    .UseNuGet("7.0.0", "https://api.nuget.org/v3/index.json")
                    .ToToolchain());
        }

        [FactEnvSpecific("We don't want to test MonoVM twice (for .NET Framework 4.6.2 and .NET 8.0)", EnvRequirement.DotNetCoreOnly)]
        public void MemoryDiagnoserSupportsModernMono()
        {
            MemoryDiagnoserIsAccurate(MonoToolchain.Mono80);
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

        [Theory(Skip = "#1542 Tiered JIT Thread allocates memory in the background"), MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(AllocatingGlobalSetupAndCleanup), new Dictionary<string, long>
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
            if (RuntimeInformation.IsFullFramework && toolchain.IsInProcess)
            {
                return; // this test is flaky on Full Framework
            }

            AssertAllocations(toolchain, typeof(NoAllocationsAtAll), new Dictionary<string, long>
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

        public class NonAllocatingAsynchronousBenchmarks
        {
            private readonly Task<int> completedTaskOfT = Task.FromResult(default(int)); // we store it in the field, because Task<T> is reference type so creating it allocates heap memory

            [Benchmark] public Task CompletedTask() => Task.CompletedTask;

            [Benchmark] public Task<int> CompletedTaskOfT() => completedTaskOfT;

            [Benchmark] public ValueTask<int> CompletedValueTaskOfT() => new ValueTask<int>(default(int));
        }

        [Theory, MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AwaitingTasksShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            if (toolchain.IsInProcess)
            {
                return; // it's flaky: https://github.com/dotnet/BenchmarkDotNet/issues/1925
            }

            AssertAllocations(toolchain, typeof(NonAllocatingAsynchronousBenchmarks), new Dictionary<string, long>
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

        [Theory(Skip = "#1542 Tiered JIT Thread allocates memory in the background"), MemberData(nameof(GetToolchains))]
        //[TheoryNetCoreOnly("Only .NET Core 2.0+ API is bug free for this case"), MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AllocationQuantumIsNotAnIssueForNetCore21Plus(IToolchain toolchain)
        {
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

        [TheoryEnvSpecific(".NET Core 3.0 preview6+ exposes a GC.GetTotalAllocatedBytes method which makes it possible to work",
            EnvRequirement.DotNetCore30Only)]
        [MemberData(nameof(GetToolchains))]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserIsAccurateForMultiThreadedBenchmarks(IToolchain toolchain)
        {
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
            var config = CreateConfig(toolchain);
            var benchmarks = BenchmarkConverter.TypeToBenchmarks(benchmarkType, config);

            var summary = BenchmarkRunner.Run(benchmarks);
            try
            {
                summary.CheckPlatformLinkerIssues();
            }
            catch (MisconfiguredEnvironmentException e)
            {
                if (ContinuousIntegration.IsLocalRun())
                {
                    output.WriteLine(e.SkipMessage);
                    return;
                }
                throw;
            }

            foreach (var benchmarkAllocationsValidator in benchmarksAllocationsValidators)
            {
                var allocatingBenchmarks = benchmarks.BenchmarksCases.Where(benchmark => benchmark.DisplayInfo.Contains(benchmarkAllocationsValidator.Key));

                foreach (var benchmark in allocatingBenchmarks)
                {
                    var benchmarkReport = summary.Reports.Single(report => report.BenchmarkCase == benchmark);

                    Assert.Equal(benchmarkAllocationsValidator.Value, benchmarkReport.GcStats.GetBytesAllocatedPerOperation(benchmark));

                    if (benchmarkAllocationsValidator.Value == 0)
                    {
                        Assert.Equal(0, benchmarkReport.GcStats.GetTotalAllocatedBytes(excludeAllocationQuantumSideEffects: true));
                    }
                }
            }
        }

        private IConfig CreateConfig(IToolchain toolchain)
            => ManualConfig.CreateEmpty()
                .AddJob(Job.ShortRun
                    .WithEvaluateOverhead(false) // no need to run idle for this test
                    .WithWarmupCount(0) // don't run warmup to save some time for our CI runs
                    .WithIterationCount(1) // single iteration is enough for us
                    .WithGcForce(false)
                    .WithGcServer(false)
                    .WithGcConcurrent(false)
                    .WithEnvironmentVariable("COMPlus_TieredCompilation", "0") // Tiered JIT can allocate some memory on a background thread, let's disable it to make our tests less flaky (#1542)
                    .WithToolchain(toolchain))
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
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
