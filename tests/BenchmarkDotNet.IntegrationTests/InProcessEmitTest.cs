using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.IntegrationTests.InProcess.EmitTests;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.Roslyn;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class InProcessEmitTest : BenchmarkTestExecutor
    {
        public InProcessEmitTest(ITestOutputHelper output) : base(output) { }

        private const decimal DecimalResult = 42;
        private const string StringResult = "42";

        private const int UnrollFactor = 16;

        private IConfig CreateInProcessConfig(OutputLogger logger = null, IDiagnoser diagnoser = null)
        {
            return new ManualConfig()
                .With(Job.Dry.With(new InProcessEmitToolchain(TimeSpan.Zero, true)).WithInvocationCount(UnrollFactor).WithUnrollFactor(UnrollFactor))
                .With(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .With(DefaultColumnProviders.Instance);
        }

        private IConfig CreateInProcessOrRoslynConfig(OutputLogger logger = null, IDiagnoser diagnoser = null)
        {
            var config = new ManualConfig();

            config.Add(DefaultConfig.Instance.GetColumnProviders().ToArray());
            config.Add(DefaultConfig.Instance.GetAnalysers().ToArray());
            config.Add(DefaultConfig.Instance.GetExporters().ToArray());
            config.Add(DefaultConfig.Instance.GetFilters().ToArray());
            config.Add(DefaultConfig.Instance.GetLoggers().ToArray());
            config.Add(
                Job.Dry
                    .With(InProcessEmitToolchain.DontLogOutput)
                    .WithInvocationCount(4)
                    .WithUnrollFactor(4));
            config.Add(
                Job.Dry
                    .With(new RoslynToolchain())
                    .WithInvocationCount(4)
                    .WithUnrollFactor(4));
            config.KeepBenchmarkFiles = true;
            config.Add(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default));

            return config;
        }

        private void DiffEmit(Summary summary)
        {
            var caseName = summary.BenchmarksCases.First().Job.ToString();
            NaiveRunnableEmitDiff.RunDiff(
                $@"{caseName}.exe",
                $@"{caseName}Emitted.dll",
                ConsoleLogger.Default);
        }

        [Fact]
        public void InProcessBenchmarkSimpleCasesReflectionEmitSupported()
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessConfig(logger);

            try
            {
                BenchmarkAllCases.Counter = 0;

                var summary = CanExecute<BenchmarkAllCases>(config);

                string testLog = logger.GetLog();
                Assert.Contains("// Benchmark: BenchmarkAllCases.InvokeOnceVoid:", testLog);
                Assert.DoesNotContain("No benchmarks found", logger.GetLog());

                // Operations + GlobalSetup + GlobalCleanup
                long expectedCount = summary.Reports.SelectMany(r => r.AllMeasurements).Sum(m => m.Operations + 2);
                Assert.Equal(expectedCount, BenchmarkAllCases.Counter);
            }
            finally
            {
                BenchmarkAllCases.Counter = 0;
            }
        }

        [Theory]
        [InlineData(typeof(SampleBenchmark))]
        [InlineData(typeof(RunnableVoidCaseBenchmark))]
        [InlineData(typeof(RunnableRefStructCaseBenchmark))]
        [InlineData(typeof(RunnableStructCaseBenchmark))]
        [InlineData(typeof(RunnableClassCaseBenchmark))]
        [InlineData(typeof(RunnableManyArgsCaseBenchmark))]
        [InlineData(typeof(RunnableTaskCaseBenchmark))]
        public void InProcessBenchmarkEmitsSameMsil(Type benchmarkType)
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessOrRoslynConfig(logger);

            var summary = CanExecute(benchmarkType, config);
#if NETFRAMEWORK
            // .Net core does not support assembly saving so far
            // SEE https://github.com/dotnet/corefx/issues/4491
            DiffEmit(summary);
#endif
            string testLog = logger.GetLog();
            Assert.Contains(benchmarkType.Name, testLog);
            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
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