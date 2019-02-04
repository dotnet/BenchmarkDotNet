using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
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
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Dry.With(InProcessEmitToolchain.Instance));

        [Fact]
        public void DryJobWithInProcessToolchainDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Dry.With(InProcessEmitToolchain.Instance));

        [Fact]
        public void DryJobWithInProcessToolchainDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Dry.With(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptions()
            => SourceExceptionMessageIsDisplayed<AlwaysThrow>(Job.Default.With(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptionsWhenIterationCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInIterationCleanup>(Job.Default.With(InProcessEmitToolchain.Instance));

        [Fact]
        public void DefaultJobWithInProcessToolchainDoesNotEatExceptionsWhenGlobalCleanupThrows()
            => SourceExceptionMessageIsDisplayed<ThrowInBenchmarkAndInGlobalCleanup>(Job.Default.With(InProcessEmitToolchain.Instance));

        [AssertionMethod]
        private void SourceExceptionMessageIsDisplayed<TBenchmark>(Job job)
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.CreateEmpty().With(job).With(logger);

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
    }
}
