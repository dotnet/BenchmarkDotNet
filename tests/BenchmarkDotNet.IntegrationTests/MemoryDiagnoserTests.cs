using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.Mono;
using BenchmarkDotNet.Toolchains.MonoAotLLVM;
using BenchmarkDotNet.Toolchains.MonoWasm;
using BenchmarkDotNet.Toolchains.NativeAot;
using BenchmarkDotNet.Validators;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MemoryDiagnoserTests
    {
        private readonly ITestOutputHelper output;

        public MemoryDiagnoserTests(ITestOutputHelper outputHelper) => output = outputHelper;

        public static IEnumerable<object[]> GetToolchains()
        {
            // xunit v2 allocates every 100ms on a background timer that makes the tests flaky on Mac/Linux (MSTestEnableParentProcessQuery).
            // TODO: remove the guard when the test framework is updated.
            if (OsDetector.IsWindows())
                yield return [InProcessEmitToolchain.Default];

            if (ContinuousIntegration.IsGitHubDraftPR())
                yield break;

            yield return [Job.Default.GetToolchain()];
        }

        public class AccurateAllocations
        {
            [Benchmark] public byte[] EightBytesArray() => new byte[8];
            [Benchmark] public byte[] SixtyFourBytesArray() => new byte[64];

            [Benchmark] public Task<int> AllocateTask() => Task.FromResult<int>(-12345);
        }

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [FactEnvSpecific("We don't want to test NativeAOT twice (.NET Framework and .NET Core)", [EnvRequirement.DotNetCoreOnly, EnvRequirement.NonGitHubDraftPR])]
        public void MemoryDiagnoserSupportsNativeAOT()
        {
            if (OsDetector.IsMacOS())
                return; // currently not supported

            MemoryDiagnoserIsAccurate(NativeAotToolchain.Net80);
        }

        [FactEnvSpecific("We don't want to test MonoVM twice (.NET Framework and .NET Core), and it's not supported on Windows+Arm",
            [EnvRequirement.DotNetCoreOnly, EnvRequirement.NonWindowsArm, EnvRequirement.NonGitHubDraftPR])]
        public void MemoryDiagnoserSupportsModernMono()
        {
            MemoryDiagnoserIsAccurate(MonoToolchain.Mono80);
        }

        [TheoryEnvSpecific("We don't want to test Wasm twice (.NET Framework and .NET Core), and JSVU does not support ARM on Windows or Linux",
            [EnvRequirement.DotNetCoreOnly, EnvRequirement.NonWindowsArm, EnvRequirement.NonLinuxArm, EnvRequirement.NonGitHubDraftPR])]
        [InlineData(MonoAotCompilerMode.mini)]
        // BUG: https://github.com/dotnet/BenchmarkDotNet/issues/3036
        [InlineData(MonoAotCompilerMode.wasm, Skip = "AOT is broken")]
        public void MemoryDiagnoserSupportsMonoWasm(MonoAotCompilerMode aotCompilerMode)
        {
            var ptrSize = sizeof(Int32); // We can't rely on IntPtr.Size, since we run on a different platform. Wasm is currently 32bit.
            var objectAllocationOverhead = ptrSize * 2; // pointer to method table + object header word
            var arraySizeOverhead = ptrSize * 2; // bounds + max_length
            var intTaskSize = 40; // We can't use CalculateRequiredSpace for AllocateTask since it calculates the size with IntPtr.Size.

            var netCoreAppSettings = new NetCoreAppSettings("net8.0", runtimeFrameworkVersion: null!, "Wasm", aotCompilerMode: aotCompilerMode);

            var runtime = new WasmRuntime(
                netCoreAppSettings.TargetFrameworkMoniker, RuntimeMoniker.WasmNet80,
                "Wasm", aotCompilerMode == MonoAotCompilerMode.wasm, "v8");

            AssertAllocations(WasmToolchain.From(netCoreAppSettings), typeof(AccurateAllocations), new Dictionary<string, long>
            {
                { nameof(AccurateAllocations.EightBytesArray), 8 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateAllocations.SixtyFourBytesArray), 64 + objectAllocationOverhead + arraySizeOverhead },
                { nameof(AccurateAllocations.AllocateTask), intTaskSize },
            }, runtime: runtime);
        }

        public class AllocatingGlobalSetupAndCleanup
        {
            private List<int> list = default!;

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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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
        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
        [Trait(Constants.Category, Constants.BackwardCompatibilityCategory)]
        public void TieredJitShouldNotInterfereAllocationResults(IToolchain toolchain)
        {
            AssertAllocations(toolchain, typeof(TimeConsumingBenchmark), new Dictionary<string, long>
            {
                { nameof(TimeConsumingBenchmark.TimeConsuming), 0 }
            },
            iterationCount: 10); // 1 iteration is not enough to repro the problem
        }

        public class NoBoxing
        {
            [Benchmark] public ValueTuple<int> ReturnsValueType() => new ValueTuple<int>(0);
        }

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [Theory(SkipTestWithoutData = true)]
        [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
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

        [TheoryEnvSpecific("Full Framework cannot measure precisely enough for low invocation counts.", EnvRequirement.DotNetCoreOnly, SkipTestWithoutData = true)]
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
            private Thread[] threads = default!;
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

        [TheoryEnvSpecific("Full Framework cannot measure precisely enough", EnvRequirement.DotNetCoreOnly, SkipTestWithoutData = true)]
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

        private void AssertAllocations(IToolchain toolchain, Type benchmarkType, Dictionary<string, long> benchmarksAllocationsValidators, int iterationCount = 1, Runtime? runtime = null)
        {
            var config = CreateConfig(toolchain, runtime, iterationCount);
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

        private IConfig CreateConfig(IToolchain toolchain, Runtime? runtime,
            // Single iteration is enough for most of the tests.
            int iterationCount = 1)
        {
            var job = Job.ShortRun
                .WithEvaluateOverhead(false) // no need to run idle for this test
                .WithWarmupCount(0) // JIT stage already warms up the method.
                .WithIterationCount(iterationCount)
                .WithGcForce(false)
                .WithGcServer(false)
                .WithGcConcurrent(false)
                .WithJitTieringMode(JitTieringMode.Force)
                .WithToolchain(toolchain);

            if (runtime is not null)
            {
                job = job.WithRuntime(runtime);
            }

            return ManualConfig.CreateEmpty()
                .AddJob(job)
                .WithBuildTimeout(TimeSpan.FromSeconds(480)) // Increase timeout for `MemoryDiagnoserSupportsModernMono` test on macos(x64)
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddDiagnoser(new FinalizerBlockerDiagnoser())
                .AddLogger(new OutputLogger(output));
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
                var sizeOf = typeof(Unsafe).GetTypeInfo().GetMethod(nameof(Unsafe.SizeOf))!;

                return (int)sizeOf.MakeGenericMethod(type).Invoke(null, null)!;
            }

            return typeof(T)
                .GetAllFields()
                .Where(field => !field.IsStatic && !field.IsLiteral)
                .Distinct()
                .Sum(field => GetSize(field.FieldType));
        }

        // To prevent finalizers interfering with allocation measurements, we block the finalizer thread during the extra iteration.
        // https://github.com/dotnet/runtime/issues/101536#issuecomment-2077647417
        public sealed class FinalizerBlockerDiagnoser : IInProcessDiagnoser
        {
            public IEnumerable<string> Ids => [nameof(FinalizerBlockerDiagnoser)];
            public IEnumerable<IExporter> Exporters => [];
            public IEnumerable<IAnalyser> Analysers => [];
            public void DeserializeResults(BenchmarkCase benchmarkCase, string serializedResults) { }
            public void DisplayResults(ILogger logger) { }
            public IAsyncEnumerable<ValidationError> ValidateAsync(ValidationParameters validationParameters) => AsyncEnumerable.Empty<ValidationError>();
            public IEnumerable<Metric> ProcessResults(DiagnoserResults results) => [];
            public ValueTask HandleAsync(HostSignal signal, DiagnoserActionParameters parameters, CancellationToken cancellationToken) => new();
            public BenchmarkDotNet.Diagnosers.RunMode GetRunMode(BenchmarkCase benchmarkCase)
                // Mono Wasm throws PlatformNotSupportedException from Monitor.Wait, and defers finalization to the JS event loop (single-threaded),
                // so it's impossible for us to prevent the finalizer from running. The good thing is that means it cannot run during our synchronous
                // benchmarks, but it also means we should never add any async-yielding benchmark memory tests for Wasm.
                => benchmarkCase.GetToolchain() is WasmToolchain
                    ? BenchmarkDotNet.Diagnosers.RunMode.None
                    : BenchmarkDotNet.Diagnosers.RunMode.ExtraIteration;
            public InProcessDiagnoserHandlerData GetHandlerData(BenchmarkCase benchmarkCase) => new(typeof(FinalizerBlockerDiagnoserHandler), null);
        }

        public sealed class FinalizerBlockerDiagnoserHandler : IInProcessDiagnoserHandler
        {
            private object? hangLock;

            private sealed class Impl
            {
                // ManualResetEvent(Slim) allocates when it is waited and yields the thread,
                // so we use Monitor.Wait instead which does not allocate managed memory.
                // This behavior is not documented, but was observed with the VS Profiler.
                private readonly object hangLock = new();
                private readonly ManualResetEventSlim enteredFinalizerEvent = new(false);

                ~Impl()
                {
                    lock (hangLock)
                    {
                        enteredFinalizerEvent.Set();
                        Monitor.Wait(hangLock);
                    }
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                internal static (object hangLock, ManualResetEventSlim enteredFinalizerEvent) CreateWeakly()
                {
                    var impl = new Impl();
                    return (impl.hangLock, impl.enteredFinalizerEvent);
                }
            }

            private void Start()
            {
                (hangLock, var enteredFinalizerEvent) = Impl.CreateWeakly();
                do
                {
                    GC.Collect();
                    // Do NOT call GC.WaitForPendingFinalizers.
                }
                while (!enteredFinalizerEvent.IsSet);
            }

            private void Stop()
            {
                lock (hangLock!)
                {
                    Monitor.Pulse(hangLock);
                }
            }

            public ValueTask HandleAsync(BenchmarkSignal signal, InProcessDiagnoserActionArgs args, CancellationToken cancellationToken)
            {
                switch (signal)
                {
                    case BenchmarkSignal.BeforeExtraIteration:
                        Start();
                        break;
                    case BenchmarkSignal.AfterExtraIteration:
                        Stop();
                        break;
                }
                return new();
            }

            public void Initialize(string? serializedConfig) { }
            public string SerializeResults() => string.Empty;
        }
    }
}
