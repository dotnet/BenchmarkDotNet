﻿using System;
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
using BenchmarkDotNet.Tests.Mocks;
using BenchmarkDotNet.Toolchains.InProcess;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable CS0618

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
        private readonly IClock clock = new MockClock(TimeInterval.Millisecond.ToFrequency());

        [Fact]
        public void BenchmarkActionGlobalSetupSupported() => TestInvoke(x => BenchmarkAllCases.GlobalSetup(), UnrollFactor);

        [Fact]
        public void BenchmarkActionGlobalCleanupSupported() => TestInvoke(x => x.GlobalCleanup(), UnrollFactor);

        [Fact]
        public void BenchmarkActionVoidSupported() => TestInvoke(x => x.InvokeOnceVoid(), UnrollFactor);

        [Fact]
        public void BenchmarkActionTaskSupported() => TestInvoke(x => x.InvokeOnceTaskAsync(), UnrollFactor, null);

        [Fact]
        public void BenchmarkActionValueTaskSupported() => TestInvoke(x => x.InvokeOnceValueTaskAsync(), UnrollFactor, null);

        [Fact]
        public void BenchmarkActionRefTypeSupported() => TestInvoke(x => x.InvokeOnceRefType(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionValueTypeSupported() => TestInvoke(x => x.InvokeOnceValueType(), UnrollFactor, DecimalResult);

        [Fact]
        public void BenchmarkActionTaskOfTSupported() => TestInvoke(x => x.InvokeOnceTaskOfTAsync(), UnrollFactor, StringResult);

        [Fact]
        public void BenchmarkActionValueTaskOfTSupported() => TestInvoke(x => x.InvokeOnceValueTaskOfT(), UnrollFactor, DecimalResult);

        [Fact]
        public void BenchmarkActionGlobalSetupTaskSupported() => TestInvokeSetupCleanupTask(x => BenchmarkSetupCleanupTask.GlobalSetup());

        [Fact]
        public void BenchmarkActionGlobalCleanupTaskSupported() => TestInvokeSetupCleanupTask(x => x.GlobalCleanup());

        [Fact]
        public void BenchmarkActionIterationSetupTaskSupported() => TestInvokeSetupCleanupTask(x => BenchmarkSetupCleanupTask.GlobalSetup());

        [Fact]
        public void BenchmarkActionIterationCleanupTaskSupported() => TestInvokeSetupCleanupTask(x => x.GlobalCleanup());

        [Fact]
        public void BenchmarkActionGlobalSetupValueTaskSupported() => TestInvokeSetupCleanupValueTask(x => BenchmarkSetupCleanupValueTask.GlobalSetup());

        [Fact]
        public void BenchmarkActionGlobalCleanupValueTaskSupported() => TestInvokeSetupCleanupValueTask(x => x.GlobalCleanup());

        [Fact]
        public void BenchmarkActionIterationSetupValueTaskSupported() => TestInvokeSetupCleanupValueTask(x => BenchmarkSetupCleanupValueTask.GlobalSetup());

        [Fact]
        public void BenchmarkActionIterationCleanupValueTaskSupported() => TestInvokeSetupCleanupValueTask(x => x.GlobalCleanup());

        [AssertionMethod]
        private void TestInvoke(Expression<Action<BenchmarkAllCases>> methodCall, int unrollFactor)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod, targetMethod, targetMethod);

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkAllCases.Counter);

            // Idle mode
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);

            // GlobalSetup/GlobalCleanup
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkAllCases());
            TestInvoke(action, 1, false, null, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkAllCases());
            TestInvoke(action, 1, false, null, ref BenchmarkAllCases.Counter);

            // GlobalSetup/GlobalCleanup (empty)
            descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod);
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkAllCases());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkAllCases());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);

            // Dummy (just in case something may broke)
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkAllCases.Counter);
        }

        [AssertionMethod]
        private void TestInvoke<T>(Expression<Func<BenchmarkAllCases, T>> methodCall, int unrollFactor, object expectedResult)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var descriptor = new Descriptor(typeof(BenchmarkAllCases), targetMethod);

            var methodReturnType = typeof(T);
            bool isAwaitable = methodReturnType == typeof(Task) || methodReturnType == typeof(ValueTask)
                || (methodReturnType.GetTypeInfo().IsGenericType
                    && (methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(Task<>)
                    || methodReturnType.GetTypeInfo().GetGenericTypeDefinition() == typeof(ValueTask<>)));
            if (isAwaitable)
            {
                unrollFactor = 1;
            }

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, expectedResult, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, expectedResult, ref BenchmarkAllCases.Counter);

            // Idle mode

            bool isValueTask = methodReturnType.IsConstructedGenericType && methodReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>);

            object idleExpected;
            if (isValueTask)
                idleExpected = GetDefault(methodReturnType.GetGenericArguments()[0]);
            else if (expectedResult == null || methodReturnType == typeof(Task) || methodReturnType == typeof(ValueTask))
                idleExpected = null;
            else if (methodReturnType.GetTypeInfo().IsValueType)
                idleExpected = 0;
            else
                idleExpected = GetDefault(expectedResult.GetType());

            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, idleExpected, ref BenchmarkAllCases.Counter);
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkAllCases(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, idleExpected, ref BenchmarkAllCases.Counter);
        }

        [AssertionMethod]
        private void TestInvokeSetupCleanupTask(Expression<Func<BenchmarkSetupCleanupTask, Task>> methodCall)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var descriptor = new Descriptor(typeof(BenchmarkSetupCleanupTask), targetMethod, targetMethod, targetMethod, targetMethod, targetMethod);
            int unrollFactor = 1;

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkSetupCleanupTask(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkSetupCleanupTask.Counter);
            action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkSetupCleanupTask(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkSetupCleanupTask.Counter);

            // Idle mode
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkSetupCleanupTask(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkSetupCleanupTask(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);

            // GlobalSetup/GlobalCleanup
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkSetupCleanupTask());
            TestInvoke(action, 1, false, null, ref BenchmarkSetupCleanupTask.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkSetupCleanupTask());
            TestInvoke(action, 1, false, null, ref BenchmarkSetupCleanupTask.Counter);

            // GlobalSetup/GlobalCleanup (empty)
            descriptor = new Descriptor(typeof(BenchmarkSetupCleanupTask), targetMethod);
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkSetupCleanupTask());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkSetupCleanupTask());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);

            // Dummy (just in case something may broke)
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupTask.Counter);
        }

        [AssertionMethod]
        private void TestInvokeSetupCleanupValueTask(Expression<Func<BenchmarkSetupCleanupValueTask, ValueTask>> methodCall)
        {
            var targetMethod = ((MethodCallExpression) methodCall.Body).Method;
            var descriptor = new Descriptor(typeof(BenchmarkSetupCleanupValueTask), targetMethod, targetMethod, targetMethod, targetMethod, targetMethod);
            int unrollFactor = 1;

            // Run mode
            var action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkSetupCleanupValueTask(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkSetupCleanupValueTask.Counter);
            action = BenchmarkActionFactory.CreateWorkload(descriptor, new BenchmarkSetupCleanupValueTask(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, false, null, ref BenchmarkSetupCleanupValueTask.Counter);

            // Idle mode
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkSetupCleanupValueTask(), BenchmarkActionCodegen.ReflectionEmit, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);
            action = BenchmarkActionFactory.CreateOverhead(descriptor, new BenchmarkSetupCleanupValueTask(), BenchmarkActionCodegen.DelegateCombine, unrollFactor);
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);

            // GlobalSetup/GlobalCleanup
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkSetupCleanupValueTask());
            TestInvoke(action, 1, false, null, ref BenchmarkSetupCleanupValueTask.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkSetupCleanupValueTask());
            TestInvoke(action, 1, false, null, ref BenchmarkSetupCleanupValueTask.Counter);

            // GlobalSetup/GlobalCleanup (empty)
            descriptor = new Descriptor(typeof(BenchmarkSetupCleanupValueTask), targetMethod);
            action = BenchmarkActionFactory.CreateGlobalSetup(descriptor, new BenchmarkSetupCleanupValueTask());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);
            action = BenchmarkActionFactory.CreateGlobalCleanup(descriptor, new BenchmarkSetupCleanupValueTask());
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);

            // Dummy (just in case something may broke)
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);
            action = BenchmarkActionFactory.CreateDummy();
            TestInvoke(action, unrollFactor, true, null, ref BenchmarkSetupCleanupValueTask.Counter);
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
        private void TestInvoke(BenchmarkAction benchmarkAction, int unrollFactor, bool isIdle, object expectedResult, ref int counter)
        {
            try
            {
                counter = 0;

                if (isIdle)
                {
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeSingle());
                    Assert.Equal(0, counter);
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeUnroll(0, clock));
                    Assert.Equal(0, counter);
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeUnroll(11, clock));
                    Assert.Equal(0, counter);
                }
                else
                {
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeSingle());
                    Assert.Equal(1, counter);
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeUnroll(0, clock));
                    Assert.Equal(1, counter);
                    Helpers.AwaitHelper.GetResult(benchmarkAction.InvokeUnroll(11, clock));
                    Assert.Equal(1 + unrollFactor * 11, counter);
                }

                Assert.Equal(benchmarkAction.LastRunResult, expectedResult);
            }
            finally
            {
                counter = 0;
            }
        }

        private IConfig CreateInProcessConfig(BenchmarkActionCodegen codegenMode, OutputLogger logger = null, IDiagnoser diagnoser = null)
        {
            return new ManualConfig()
                .AddJob(Job.Dry.WithToolchain(new InProcessToolchain(TimeSpan.Zero, codegenMode, true)).WithInvocationCount(UnrollFactor).WithUnrollFactor(UnrollFactor))
                .AddLogger(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .AddColumnProvider(DefaultColumnProviders.Instance);
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
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class BenchmarkSetupCleanupTask
        {
            public static int Counter;

            [GlobalSetup]
            public static async Task GlobalSetup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [GlobalCleanup]
            public async Task GlobalCleanup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [IterationSetup]
            public static async Task IterationSetup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [IterationCleanup]
            public async Task IterationCleanup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public void InvokeOnceVoid()
            {
                Interlocked.Increment(ref Counter);
            }
        }

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        public class BenchmarkSetupCleanupValueTask
        {
            public static int Counter;

            [GlobalSetup]
            public static async ValueTask GlobalSetup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [GlobalCleanup]
            public async ValueTask GlobalCleanup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [IterationSetup]
            public static async ValueTask IterationSetup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [IterationCleanup]
            public async ValueTask IterationCleanup()
            {
                await Task.Yield();
                Interlocked.Increment(ref Counter);
            }

            [Benchmark]
            public void InvokeOnceVoid()
            {
                Interlocked.Increment(ref Counter);
            }
        }
    }
}