using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;


namespace BenchmarkDotNet.IntegrationTests
{
    public class InProcessTest : BenchmarkTestExecutor
    {
        public InProcessTest(ITestOutputHelper output) : base(output)
        {
        }

        private const decimal DecimalResult = 42;
        private const string StringResult = "42";

        private const int UnrollFactor = 16;

        [Fact]
        public async Task BenchmarkActionGlobalSetupSupported() => await TestInvoke(x => BenchmarkAllCases.GlobalSetup(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionGlobalCleanupSupported() => await TestInvoke(x => x.GlobalCleanup(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionVoidSupported() => await TestInvoke(x => x.InvokeOnceVoid(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionTaskSupported() => await TestInvoke(x => x.InvokeOnceTaskAsync(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionValueTaskSupported() => await TestInvoke(x => x.InvokeOnceValueTaskAsync(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionRefTypeSupported() => await TestInvoke(x => x.InvokeOnceRefType(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionValueTypeSupported() => await TestInvoke(x => x.InvokeOnceValueType(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionTaskOfTSupported() => await TestInvoke(x => x.InvokeOnceTaskOfTAsync(), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionValueTaskOfTSupported() => await TestInvoke(x => x.InvokeOnceValueTaskOfT(), UnrollFactor);

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        [Fact]
        public unsafe void BenchmarkActionVoidPointerSupported() => TestInvoke(x => x.InvokeOnceVoidPointerType(), UnrollFactor).GetAwaiter().GetResult();
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        // Can't use ref returns in expression, so pass the MethodInfo directly instead.
        [Fact]
        public async Task BenchmarkActionByRefTypeSupported() => await TestInvoke(typeof(BenchmarkAllCases).GetMethod(nameof(BenchmarkAllCases.InvokeOnceByRefType)), UnrollFactor);

        [Fact]
        public async Task BenchmarkActionByRefReadonlyValueTypeSupported() => await TestInvoke(typeof(BenchmarkAllCases).GetMethod(nameof(BenchmarkAllCases.InvokeOnceByRefReadonlyType)), UnrollFactor);

        [Fact]
        public void BenchmarkDifferentPlatformReturnsValidationError()
        {
            var otherPlatform = IntPtr.Size == 8
                ? Platform.X86
                : Platform.X64;

            var otherPlatformConfig = new ManualConfig()
                .AddJob(Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default).WithPlatform(otherPlatform))
                .AddLogger(new OutputLogger(Output))
                .AddColumnProvider(DefaultColumnProviders.Instance);

            var runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(BenchmarkAllCases), otherPlatformConfig);
            var summary = BenchmarkRunner.Run(runInfo);

            Assert.NotEmpty(summary.ValidationErrors);
        }

        [AssertionMethod]
        private async Task TestInvoke(Expression<Action<BenchmarkAllCases>> methodCall, int unrollFactor)
        {
            var targetMethod = ((MethodCallExpression)methodCall.Body).Method;
            var descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod, targetMethod, targetMethod);

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), unrollFactor);
            await TestInvoke(action, unrollFactor, false);

            // Idle mode
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), unrollFactor);
            await TestInvoke(action, unrollFactor, true);

            // GlobalSetup/GlobalCleanup
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkAllCases());
            await TestInvoke(action, 1, false);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkAllCases());
            await TestInvoke(action, 1, false);

            // GlobalSetup/GlobalCleanup (empty)
            descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod);
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkAllCases());
            await TestInvoke(action, unrollFactor, true);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkAllCases());
            await TestInvoke(action, unrollFactor, true);
        }

        [AssertionMethod]
        private async Task TestInvoke<T>(Expression<Func<BenchmarkAllCases, T>> methodCall, int unrollFactor)
        {
            var targetMethod = ((MethodCallExpression)methodCall.Body).Method;
            await TestInvoke(targetMethod, unrollFactor);
        }

        [AssertionMethod]
        private async Task TestInvoke(MethodInfo targetMethod, int unrollFactor)
        {
            var descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod);

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), unrollFactor);
            await TestInvoke(action, unrollFactor, false);

            // Idle mode
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), unrollFactor);
            await TestInvoke(action, unrollFactor, true);
        }

        [AssertionMethod]
        private async Task TestInvoke(BenchmarkAction benchmarkAction, int unrollFactor, bool isIdle)
        {
            try
            {
                BenchmarkAllCases.Counter = 0;

                IClock clock = new MockClock(Frequency.MHz);

                if (isIdle)
                {
                    await benchmarkAction.InvokeSingle();
                    Assert.Equal(0, BenchmarkAllCases.Counter);
                    await benchmarkAction.InvokeUnroll(0, clock);
                    Assert.Equal(0, BenchmarkAllCases.Counter);
                    await benchmarkAction.InvokeUnroll(11, clock);
                    Assert.Equal(0, BenchmarkAllCases.Counter);
                }
                else
                {
                    await benchmarkAction.InvokeSingle();
                    Assert.Equal(1, BenchmarkAllCases.Counter);
                    await benchmarkAction.InvokeUnroll(0, clock);
                    Assert.Equal(1, BenchmarkAllCases.Counter);
                    await benchmarkAction.InvokeUnroll(11, clock);
                    Assert.Equal(BenchmarkAllCases.Counter, 1 + unrollFactor * 11);
                }
            }
            finally
            {
                BenchmarkAllCases.Counter = 0;
            }
        }

        private IConfig CreateInProcessConfig(OutputLogger? logger = null)
        {
            return new ManualConfig()
                .AddJob(Job.Dry.WithToolchain(InProcessNoEmitToolchain.Default).WithInvocationCount(UnrollFactor).WithUnrollFactor(UnrollFactor))
                .AddLogger(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .AddColumnProvider(DefaultColumnProviders.Instance);
        }

        [Fact]
        public void InProcessBenchmarkAllCasesSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessConfig(logger);

            try
            {
                BenchmarkAllCases.Counter = 0;

                var summary = CanExecute<BenchmarkAllCases>(config);

                var testLog = logger.GetLog();
                Assert.Contains("// Benchmark: BenchmarkAllCases.InvokeOnceVoid:", testLog);
                Assert.DoesNotContain("No benchmarks found", logger.GetLog());

                // Operations + GlobalSetup + GlobalCleanup
                long expectedCount = summary.Reports
                    .SelectMany(r => r.AllMeasurements)
                    .Where(m => m.IterationStage != IterationStage.Result)
                    .Sum(m => m.Operations + 2);
                Assert.Equal(expectedCount, BenchmarkAllCases.Counter);
            }
            finally
            {
                BenchmarkAllCases.Counter = 0;
            }
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class BenchmarkAllCases
        {
            public static int Counter;

            [GlobalSetup]
            public static void GlobalSetup()
            {
                Interlocked.Increment(ref Counter);
            }

            [GlobalCleanup]
            public void GlobalCleanup() => Interlocked.Increment(ref Counter);

            [Benchmark]
            public void InvokeOnceVoid()
            {
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public async Task InvokeOnceTaskAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public async ValueTask InvokeOnceValueTaskAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public string InvokeOnceRefType()
            {
                Interlocked.Increment(ref Counter);
                return StringResult;
            }

            [Benchmark]
            public decimal InvokeOnceValueType()
            {
                Interlocked.Increment(ref Counter);
                return DecimalResult;
            }

            [Benchmark]
            public async Task<string> InvokeOnceTaskOfTAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
                return StringResult;
            }

            [Benchmark]
            public ValueTask<decimal> InvokeOnceValueTaskOfT()
            {
                Interlocked.Increment(ref Counter);
                return new ValueTask<decimal>(DecimalResult);
            }

            [Benchmark]
            public ref int InvokeOnceByRefType()
            {
                Interlocked.Increment(ref Counter);
                return ref Counter;
            }

            [Benchmark]
            public ref readonly int InvokeOnceByRefReadonlyType()
            {
                Interlocked.Increment(ref Counter);
                return ref Counter;
            }

            [Benchmark]
            public unsafe void* InvokeOnceVoidPointerType()
            {
                Interlocked.Increment(ref Counter);
                return default;
            }

#if NET9_0_OR_GREATER
            [Benchmark]
            public Span<int> InvokeOnceRefStruct()
            {
                Interlocked.Increment(ref Counter);
                return default;
            }

            // This doesn't make much sense in practice, but the type system allows it, so we test it.
            [Benchmark]
            public ref Span<int> InvokeOnceByRefRefStruct()
            {
                Interlocked.Increment(ref Counter);
                return ref Unsafe.NullRef<Span<int>>();
            }

            [Benchmark]
            public ref readonly Span<int> InvokeOnceByRefReadonlyRefStruct()
            {
                Interlocked.Increment(ref Counter);
                return ref Unsafe.NullRef<Span<int>>();
            }
#endif
        }


#if NET8_0_OR_GREATER
        [Fact]
        public void ParamsSupportRequiredProperty()
        {
            var config = CreateInProcessConfig();
            CanExecute<ParamsTestRequiredProperty>(config);
        }

        public class ParamsTestRequiredProperty
        {
            private const string Expected = "a";

            [Params(Expected)]
            public required string ParamProperty { get; set; }

            [Benchmark]
            public void Benchmark() => Assert.Equal(Expected, ParamProperty);
        }
#endif
    }
}