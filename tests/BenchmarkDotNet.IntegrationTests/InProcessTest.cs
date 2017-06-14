using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess;
using JetBrains.Annotations;
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
        public void BenchmarkActionGlobalSetupSupported() => TestInvoke(x => BenchmarkAllCases.GlobalSetup(), UnrollFactor);

        [Fact]
        public void BenchmarkActionGlobalCleanupSupported() => TestInvoke(x => x.GlobalCleanup(), UnrollFactor);

        [Fact]
        public void BenchmarkActionVoidSupported() => TestInvoke(x => x.InvokeOnceVoid(), UnrollFactor);

        [Fact]
        public void BenchmarkActionTaskSupported() => TestInvoke(x => x.InvokeOnceTaskAsync(), UnrollFactor, null);

        [Fact]
        public void BenchmarkActionRefTypeSupported() => TestInvoke(x => x.InvokeOnceRefType(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionValueTypeSupported() => TestInvoke(x => x.InvokeOnceValueType(), UnrollFactor, DecimalResult);

        [Fact]
        public void BenchmarkActionTaskOfTSupported() => TestInvoke(x => x.InvokeOnceTaskOfTAsync(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionValueTaskOfTSupported() => TestInvoke(x => x.InvokeOnceValueTaskOfT(), UnrollFactor, DecimalResult);

        [Fact]
        public void BenchmarkActionStaticVoidSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticVoid(), UnrollFactor);

        [Fact]
        public void BenchmarkActionStaticTaskSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticTaskAsync(), UnrollFactor, null);

        [Fact]
        public void BenchmarkActionStaticRefTypeSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticRefType(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionStaticValueTypeSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticValueType(), UnrollFactor, DecimalResult);

        [Fact]
        public void BenchmarkActionStaticTaskOfTSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticTaskOfTAsync(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionStaticValueTaskOfTSupported() => TestInvoke(x => BenchmarkAllCases.InvokeOnceStaticValueTaskOfT(), UnrollFactor, DecimalResult);

        [AssertionMethod]
        private void TestInvoke(Expression<Action<BenchmarkAllCases>> methodCall, int unrollFactor)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var target = new Target(typeof(BenchmarkAllCases), targetMethod, targetMethod, targetMethod);

            // Run mode
            var action = BenchmarkActionFactory.CreateRun(target, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, null);
            action = BenchmarkActionFactory.CreateRun(target, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, null);

            // Idle mode
            action = BenchmarkActionFactory.CreateIdle(target, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, null);
            action = BenchmarkActionFactory.CreateIdle(target, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, null);

            // GlobalSetup/GlobalCleanup
            action = BenchmarkActionFactory.CreateGlobalSetup(target, new BenchmarkAllCases());
            TestInvoke(action, 1, false, null);
            action = BenchmarkActionFactory.CreateGlobalCleanup(target, new BenchmarkAllCases());
            TestInvoke(action, 1, false, null);

            // GlobalSetup/GlobalCleanup (empty)
            target = new Target(typeof(BenchmarkAllCases), targetMethod);
            action = BenchmarkActionFactory.CreateGlobalSetup(target, new BenchmarkAllCases());
            TestInvoke(action, unrollFactor, true, null);
            action = BenchmarkActionFactory.CreateGlobalCleanup(target, new BenchmarkAllCases());
            TestInvoke(action, unrollFactor, true, null);

            // Dummy (just in case something may broke)
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null);
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null);
        }

        [AssertionMethod]
        private void TestInvoke<T>(Expression<Func<BenchmarkAllCases, T>> methodCall, int unrollFactor, object expectedResult)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var target = new Target(typeof(BenchmarkAllCases), targetMethod);

            // Run mode
            var action = BenchmarkActionFactory.CreateRun(target, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, expectedResult);
            action = BenchmarkActionFactory.CreateRun(target, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, expectedResult);

            // Idle mode

            bool isValueTask = typeof(T).IsConstructedGenericType && typeof(T).GetGenericTypeDefinition() == typeof(ValueTask<>);

            object idleExpected;
            if (isValueTask)
                idleExpected = GetDefault(typeof(T).GetGenericArguments()[0]);
            else if (typeof(T).GetTypeInfo().IsValueType)
                idleExpected = 0;
            else if (expectedResult == null || typeof(T) == typeof(Task))
                idleExpected = null;
            else
                idleExpected = GetDefault(expectedResult.GetType());

            action = BenchmarkActionFactory.CreateIdle(target, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, idleExpected);
            action = BenchmarkActionFactory.CreateIdle(target, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, idleExpected);
        }

        private static object GetDefault(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        [AssertionMethod]
        private void TestInvoke(BenchmarkAction benchmarkAction, int unrollFactor, bool isIdle, object expectedResult)
        {
            try
            {
                BenchmarkAllCases.Counter = 0;

                if (isIdle)
                {
                    benchmarkAction.InvokeSingle();
                    Assert.Equal(BenchmarkAllCases.Counter, 0);
                    benchmarkAction.InvokeMultiple(0);
                    Assert.Equal(BenchmarkAllCases.Counter, 0);
                    benchmarkAction.InvokeMultiple(11);
                    Assert.Equal(BenchmarkAllCases.Counter, 0);
                }
                else
                {
                    benchmarkAction.InvokeSingle();
                    Assert.Equal(BenchmarkAllCases.Counter, 1);
                    benchmarkAction.InvokeMultiple(0);
                    Assert.Equal(BenchmarkAllCases.Counter, 1);
                    benchmarkAction.InvokeMultiple(11);
                    Assert.Equal(BenchmarkAllCases.Counter, 1 + unrollFactor * 11);
                }

                Assert.Equal(benchmarkAction.LastRunResult, expectedResult);
            }
            finally
            {
                BenchmarkAllCases.Counter = 0;
            }
        }

        private IConfig CreateInProcessConfig(BenchmarkActionCodegen codegenMode, OutputLogger logger = null, IDiagnoser diagnoser = null)
        {
            return new ManualConfig()
                .With(Job.Dry.With(new InProcessToolchain(TimeSpan.Zero, codegenMode, true)).WithInvocationCount(UnrollFactor).WithUnrollFactor(UnrollFactor))
                .With(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .With(DefaultColumnProviders.Instance);
        }

        [Fact]
        public void InProcessBenchmarkAllCasesReflectionEmitSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessConfig(BenchmarkActionCodegen.ReflectionEmit, logger);

            try
            {
                BenchmarkAllCases.Counter = 0;

                var summary = CanExecute<BenchmarkAllCases>(config);

                var testLog = logger.GetLog();
                Assert.Contains("// Benchmark: BenchmarkAllCases.InvokeOnceVoid:", testLog);
                Assert.DoesNotContain("No benchmarks found", logger.GetLog());

                // Operations + GlobalSetup + GlobalCleanup
                var expectedCount = summary.Reports.SelectMany(r => r.AllMeasurements).Sum(m => m.Operations + 2);
                Assert.Equal(expectedCount, BenchmarkAllCases.Counter);
            }
            finally
            {
                BenchmarkAllCases.Counter = 0;
            }
        }

        [Fact]
        public void InProcessBenchmarkAllCasesDelegateCombineSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessConfig(BenchmarkActionCodegen.DelegateCombine, logger);

            try
            {
                BenchmarkAllCases.Counter = 0;

                var summary = CanExecute<BenchmarkAllCases>(config);

                var testLog = logger.GetLog();
                Assert.Contains("// Benchmark: BenchmarkAllCases.InvokeOnceVoid:", testLog);
                Assert.DoesNotContain("No benchmarks found", logger.GetLog());

                // Operations + GlobalSetup + GlobalCleanup
                var expectedCount = summary.Reports.SelectMany(r => r.AllMeasurements).Sum(m => m.Operations + 2);
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
            public static void InvokeOnceStaticVoid()
            {
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public static async Task InvokeOnceStaticTaskAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public static string InvokeOnceStaticRefType()
            {
                Interlocked.Increment(ref Counter);
                return StringResult;
            }

            [Benchmark]
            public static decimal InvokeOnceStaticValueType()
            {
                Interlocked.Increment(ref Counter);
                return DecimalResult;
            }

            [Benchmark]
            public static async Task<string> InvokeOnceStaticTaskOfTAsync()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
                return StringResult;
            }

            [Benchmark]
            public static ValueTask<decimal> InvokeOnceStaticValueTaskOfT()
            {
                Interlocked.Increment(ref Counter);
                return new ValueTask<decimal>(DecimalResult);
            }
        }
    }
}