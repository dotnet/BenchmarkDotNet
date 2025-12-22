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
using BenchmarkDotNet.Detectors;
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
            yield return new object[] { Job.Default.GetToolchain() };
            // InProcessEmit reports flaky allocations in current .Net 8.
            if (!RuntimeInformation.IsNetCore)
            {
                yield return new object[] { InProcessEmitToolchain.Instance };
            }
        }

        public class AccurateAllocations
        {
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];

            [Benchmark] public Task<int> AllocateTask() => Task.FromResult<int>(-12345);
        }

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [FactEnvSpecific("We don't want to test NativeAOT twice (for .NET Framework 4.6.2 and .NET 8.0)", EnvRequirement.DotNetCoreOnly)]
        public void MemoryDiagnoserSupportsNativeAOT()
        {
            if (OsDetector.IsMacOS())
                return; // currently not supported

            MemoryDiagnoserIsAccurate(NativeAotToolchain.Net80);
        }

        [FactEnvSpecific("We don't want to test MonoVM twice (for .NET Framework 4.6.2 and .NET 8.0), and it's not supported on Windows+Arm", [EnvRequirement.DotNetCoreOnly, EnvRequirement.NonWindowsArm])]
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

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserDoesNotIncludeAllocationsFromSetupAndCleanup(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(AllocatingGlobalSetupAndCleanup), new Dictionary<string, long>
            {
                { nameof(AllocatingGlobalSetupAndCleanup.AllocateNothing), 0 }
            });
        }

        public class EmptyBenchmark
        {
            [Benchmark] public void EmptyMethod() { }
        }

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void EngineShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(EmptyBenchmark), new Dictionary<string, long>
            {
                { nameof(EmptyBenchmark.EmptyMethod), 0 }
            });
        }

        public class TimeConsumingBenchmark
        {
            [Benchmark]
            public ulong TimeConsuming()
            {
                var r = 1ul;
                for (var i = 0; i < 50_000_000; i++)
                {
                    r /= 1;
                }
                return r;
            }
        }

        // #1542
        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TieredJitShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(TimeConsumingBenchmark), new Dictionary<string, long>
            {
                { nameof(TimeConsumingBenchmark.TimeConsuming), 0 }
            },
            disableTieredJit: false, iterationCount: 10); // 1 iteration is not enough to repro the problem
        }

        public class NoBoxing
        {
            [Benchmark] public ValueTuple<int> ReturnsValueType() => new ValueTuple<int>(0);
        }

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [TheoryEnvSpecific("Full Framework cannot measure precisely enough for low invocation counts.", EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void AllocationQuantumIsNotAnIssueForNetCore21Plus(IToolchain toolchain)
        {
            // TODO: Skip test on macos. Temporary workaround for https://github.com/dotnet/BenchmarkDotNet/issues/2779
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
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
            public const int Size = 1024;
            public const int ThreadsCount = 10;

            // We cache the threads in GlobalSetup and reuse them for each benchmark invocation
            // to avoid measuring the cost of thread start and join, which varies across different runtimes.
            private Thread[] threads;
            private volatile bool keepRunning = true;
            private readonly Barrier barrier = new(ThreadsCount + 1);
            private readonly CountdownEvent countdownEvent = new(ThreadsCount);

            [GlobalSetup]
            public void Setup()
            {
                threads = Enumerable.Range(0, ThreadsCount)
                    .Select(_ => new Thread(() =>
                    {
                        while (keepRunning)
                        {
                            barrier.SignalAndWait();
                            GC.KeepAlive(new byte[Size]);
                            countdownEvent.Signal();
                        }
                    }))
                    .ToArray();
                foreach (var thread in threads)
                {
                    thread.Start();
                }
            }

            [GlobalCleanup]
            public void Cleanup()
            {
                countdownEvent.Reset(ThreadsCount);
                keepRunning = false;
                barrier.SignalAndWait();
                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            [Benchmark]
            public void Allocate()
            {
                countdownEvent.Reset(ThreadsCount);
                barrier.SignalAndWait();
                countdownEvent.Wait();
            }
        }

        [TheoryEnvSpecific("Full Framework cannot measure precisely enough", EnvRequirement.DotNetCoreOnly)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void MemoryDiagnoserIsAccurateForMultiThreadedBenchmarks(IToolchain toolchain)
        {
            long objectAllocationOverhead = IntPtr.Size * 2; // pointer to method table + object header word
            long arraySizeOverhead = IntPtr.Size; // array length
            long memoryAllocatedPerArray = (MultiThreadedAllocation.Size + objectAllocationOverhead + arraySizeOverhead);

            AssertAllocations(toolchain, typeof(MultiThreadedAllocation), new Dictionary<string, long>
            {
                { nameof(MultiThreadedAllocation.Allocate), memoryAllocatedPerArray * MultiThreadedAllocation.ThreadsCount }
            });
        }

        private void AssertAllocations(IToolchain toolchain, Type benchmarkType, Dictionary<string, long> benchmarksAllocationsValidators, bool disableTieredJit = true, int iterationCount = 1)
        {
            var config = CreateConfig(toolchain, disableTieredJit, iterationCount);
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

        private IConfig CreateConfig(IToolchain toolchain,
            // Tiered JIT can allocate some memory on a background thread, let's disable it by default to make our tests less flaky (#1542).
            // This was mostly fixed in net7.0, but tiered jit thread is not guaranteed to not allocate, so we disable it just in case.
            bool disableTieredJit = true,
            // Single iteration is enough for most of the tests.
            int iterationCount = 1,
            // Don't run warmup by default to save some time for our CI runs
            int warmupCount = 0)
        {
            var job = Job.ShortRun
                .WithEvaluateOverhead(false) // no need to run idle for this test
                .WithWarmupCount(warmupCount)
                .WithIterationCount(iterationCount)
                .WithGcForce(false)
                .WithGcServer(false)
                .WithGcConcurrent(false)
                // To prevent finalizers allocating out of our control, we hang the finalizer thread.
                // https://github.com/dotnet/runtime/issues/101536#issuecomment-2077647417
                .WithEnvironmentVariable(Engines.Engine.UnitTestBlockFinalizerEnvKey, Engines.Engine.UnitTestBlockFinalizerEnvValue)
                .WithToolchain(toolchain);
            return ManualConfig.CreateEmpty()
                .AddJob(disableTieredJit
                    ? job.WithEnvironmentVariables(
                        new EnvironmentVariable("DOTNET_TieredCompilation", "0"),
                        new EnvironmentVariable("COMPlus_TieredCompilation", "0")
                    )
                    : job)
                .WithBuildTimeout(TimeSpan.FromSeconds(240)) // Increase timeout for `MemoryDiagnoserSupportsModernMono` test on macos(x64)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddLogger(toolchain.IsInProcess ? ConsoleLogger.Default : new OutputLogger(output)); // we can't use OutputLogger for the InProcess toolchains because it allocates memory on the same thread
        }

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
