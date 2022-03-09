using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ExceptionHandlingTests : BenchmarkTestExecutor
    {
        private const string BenchmarkExceptionMessage = "we have a problem in our benchmark method";
        private const string IterationCleanupExceptionMessage = "we have a problem in our iteration cleanup method";
        private const string GlobalCleanupExceptionMessage = "we have a problem in our global cleanup method";

        public ExceptionHandlingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void DryJobDoesNotEatExceptions()
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Dry);

        [Fact]
        public void DryJobDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Dry);

        [Fact]
        public void DryJobDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Dry);

        [Fact]
        public void DefaultJobDoesNotEatExceptions()
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Default);

        [Fact]
        public void DefaultJobDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Default);

        [Fact]
        public void DefaultJobDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Default);

        [Fact]
        public void DryJobWithInProcessToolchainDoesNotEatExceptions()
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Dry.WithToolchain(InProcessEmitToolchain.Instance));

        [Fact]
        public void DryJobWithInProcessToolchainDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Dry.WithToolchain(InProcessEmitToolchain.Instance));

        [Fact]
        public void DryJobWithInProcessToolchainDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Dry.WithToolchain(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptions()
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        [AssertionMethod]
        private void SourceExceptionMessageIsDisplayed<TBenchmark>(Job job)
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.CreateEmpty().AddJob(job).AddLogger(logger);

            CanExecute<TBenchmark>(config, fullValidation: false); // we don't validate here because the report is expected to have no results

            Assert.Contains(BenchmarkExceptionMessage, logger.GetLog());
        }

        public class AlwaysThrow
        {
            [Benchmark] public void Throw() => throw new Exception(BenchmarkExceptionMessage);
        }

        public class ThrowInBenchmarkAndInGlobalCleanup
        {
            [GlobalCleanup] public void Cleanup() => throw new Exception(GlobalCleanupExceptionMessage);

            [Benchmark] public void Throw() => throw new Exception(BenchmarkExceptionMessage);
        }

        public class ThrowInBenchmarkAndInIterationCleanup
        {
            [IterationCleanup] public void Cleanup() => throw new Exception(IterationCleanupExceptionMessage);

            [Benchmark] public void Throw() => throw new Exception(BenchmarkExceptionMessage);
        }

        [Fact]
        public void WhenOneBenchmarkThrowsTheRunnerDoesNotThrow()
            => CanExecute<OneIsThrowing>(fullValidation: false); // we don't validate here because the report is expected to miss some results

        [Fact]
        public void WhenAllBenchmarksThrowsTheRunnerDoesNotThrow()
            => CanExecute<AlwaysThrow>(fullValidation: false); // we don't validate here because the report is expected to have no results

        public class OneIsThrowing
        {
            [Benchmark(Baseline = true)]
            public void Bar1() { }
            [Benchmark]
            public void Bar2() => throw new Exception();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StopOnFirstErrorIsRespected(bool value)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry)
                .AddDiagnoser(MemoryDiagnoser.Default) // crucial to repro the bug
                .WithOption(ConfigOptions.StopOnFirstError, value);

            var summary = CanExecute<MoreThanOneNonThrowingBenchmark>(config, fullValidation: false);

            if (value)
            {
                Assert.Equal(1, summary.Reports.Count(report => !report.Success));
            }
            else
            {
                Assert.Equal(3, summary.Reports.Count(report => report.Success));
                Assert.Equal(4, summary.Reports.Count(report => !report.Success));
            }
        }

        public class MoreThanOneNonThrowingBenchmark
        {
            [Benchmark] public void Ok1() { }
            [Benchmark] public void Throw1() => throw new Exception(BenchmarkExceptionMessage);
            [Benchmark] public void Ok2() { }
            [Benchmark] public void Throw2() => throw new Exception(BenchmarkExceptionMessage);
            [Benchmark] public void Ok3() { }
            [Benchmark] public void Throw3() => throw new Exception(BenchmarkExceptionMessage);
            [Benchmark] public void Throw4() => throw new Exception(BenchmarkExceptionMessage);
        }

        [Fact]
        public void DiagnosersCantCrashRunnerWhenTheyHaveNothingToParse()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry.WithIterationCount(10))
                .AddDiagnoser(MemoryDiagnoser.Default); // crucial to repro the bug;

            var summary = CanExecute<ThrowButNotImmediately>(config, fullValidation: false);

            var benchmarkReport = summary.Reports.Single();
            Assert.Equal(default, benchmarkReport.GcStats);
        }

        public class ThrowButNotImmediately
        {
            private int iterationCount;

            [IterationSetup]
            public void Setup() => ++iterationCount;

            [Benchmark]
            public void Throwing()
            {
                if (iterationCount >= 5)
                {
                    throw new Exception();
                }
            }
        }
    }
}
