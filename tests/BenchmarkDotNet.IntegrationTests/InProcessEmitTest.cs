using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.IntegrationTests.InProcess.EmitTests;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Tests.XUnit;
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

        private IConfig CreateInProcessConfig(OutputLogger logger)
        {
            return new ManualConfig()
                .AddJob(Job.Dry.WithToolchain(new InProcessEmitToolchain(TimeSpan.Zero, true)).WithInvocationCount(UnrollFactor).WithUnrollFactor(UnrollFactor))
                .AddLogger(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default))
                .AddColumnProvider(DefaultColumnProviders.Instance);
        }

        private IConfig CreateInProcessAndRoslynConfig(OutputLogger logger)
        {
            var config = new ManualConfig()
                .AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray())
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
                .AddExporter(DefaultConfig.Instance.GetExporters().ToArray())
                .AddFilter(DefaultConfig.Instance.GetFilters().ToArray())
                .AddLogger(DefaultConfig.Instance.GetLoggers().ToArray())
                .AddJob(
                    Job.Dry
                        .WithToolchain(InProcessEmitToolchain.DontLogOutput)
                        .WithInvocationCount(4)
                        .WithUnrollFactor(4))
                .AddJob(
                    Job.Dry
                        .WithToolchain(new RoslynToolchain())
                        .WithInvocationCount(4)
                        .WithUnrollFactor(4))
                .WithOptions(ConfigOptions.KeepBenchmarkFiles)
                .AddLogger(logger ?? (Output != null ? new OutputLogger(Output) : ConsoleLogger.Default));

            return config;
        }

        private void DiffEmit(Summary summary)
        {
            // .Net Core does not support assembly saving so far
            // SEE https://github.com/dotnet/corefx/issues/4491
            if (!Portability.RuntimeInformation.IsFullFramework)
                return;

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

        [TheoryEnvSpecific("We can't use Roslyn toolchain for .NET Core because we don't know which assemblies to reference and .NET Core does not support dynamic assembly saving", EnvRequirement.FullFrameworkOnly)]
        [InlineData(typeof(SampleBenchmark))]
        [InlineData(typeof(RunnableVoidCaseBenchmark))]
        [InlineData(typeof(RunnableRefStructCaseBenchmark))]
        [InlineData(typeof(RunnableStructCaseBenchmark))]
        [InlineData(typeof(RunnableClassCaseBenchmark))]
        [InlineData(typeof(RunnableManyArgsCaseBenchmark))]
        [InlineData(typeof(RunnableTaskCaseBenchmark))]
        public void InProcessBenchmarkEmitsSameIL(Type benchmarkType)
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessAndRoslynConfig(logger);

            var summary = CanExecute(benchmarkType, config);

            DiffEmit(summary);

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

        [Fact]
        public void InProcessEmitToolchainSupportsIterationSetupAndCleanup()
        {
            var logger = new OutputLogger(Output);
            var config = CreateInProcessConfig(logger);

            WithIterationSetupAndCleanup.SetupCounter = 0;
            WithIterationSetupAndCleanup.BenchmarkCounter = 0;
            WithIterationSetupAndCleanup.CleanupCounter = 0;

            var summary = CanExecute<WithIterationSetupAndCleanup>(config);

            Assert.Equal(1, WithIterationSetupAndCleanup.SetupCounter);
            Assert.Equal(16, WithIterationSetupAndCleanup.BenchmarkCounter);
            Assert.Equal(1, WithIterationSetupAndCleanup.CleanupCounter);
        }

        public class WithIterationSetupAndCleanup
        {
            public static int SetupCounter, BenchmarkCounter, CleanupCounter;

            [IterationSetup]
            public void Setup() => Interlocked.Increment(ref SetupCounter);

            [Benchmark]
            public void Benchmark() => Interlocked.Increment(ref BenchmarkCounter);

            [IterationCleanup]
            public void Cleanup() => Interlocked.Increment(ref CleanupCounter);
        }
    }
}