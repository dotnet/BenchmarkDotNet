using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

// BDN1700 is the analyzer counterpart of the rejection branch in SetupCleanupValidator. The test types
// below intentionally return async enumerables from setup/cleanup, so suppress the analyzer in this file
// — we are validating the runtime validator's reject behavior, not the analyzer.
#pragma warning disable BDN1700

namespace BenchmarkDotNet.Tests.Validators
{
    public class SetupCleanupValidatorTests
    {
        public class BlankTargetClass
        {
            [GlobalSetup] public void SetupA() { }
            [GlobalSetup] public void SetupB() { }
            [Benchmark] public void Benchmark() { }
        }

        [Fact]
        public async Task InvalidGlobalSetupTooManyBlankTargets()
        {
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(BlankTargetClass))).ToArrayAsync();

            var count = validationErrors.Count(v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("Blank"));

            Assert.Equal(1, count);
        }

        public class ExplicitTargetClass
        {
            [GlobalSetup(Target = nameof(Benchmark))] public void SetupA() { }
            [GlobalSetup(Target = nameof(Benchmark))] public void SetupB() { }
            [Benchmark] public void Benchmark() { }
        }

        [Fact]
        public async Task InvalidGlobalSetupTooManyExplicitTargets()
        {
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(ExplicitTargetClass))).ToArrayAsync();

            var count = validationErrors.Count(v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("Target = Benchmark"));

            Assert.Equal(1, count);
        }

        public class AsyncEnumerableGlobalSetupClass
        {
            [GlobalSetup]
            public async IAsyncEnumerable<int> Setup()
            {
                await Task.Yield();
                yield return 1;
            }

            [Benchmark]
            public void Benchmark() { }
        }

        [Fact]
        public async Task AsyncEnumerableGlobalSetupIsRejected()
        {
            // Setup/cleanup methods returning IAsyncEnumerable would be silently dropped by the framework
            // (only awaitables are awaited there). Surface this clearly at validation time.
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableGlobalSetupClass))).ToArrayAsync();

            Assert.Contains(validationErrors, v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("async enumerable"));
        }

        public class AsyncEnumerableGlobalCleanupClass
        {
            [GlobalCleanup]
            public async IAsyncEnumerable<int> Cleanup()
            {
                await Task.Yield();
                yield return 1;
            }

            [Benchmark]
            public void Benchmark() { }
        }

        [Fact]
        public async Task AsyncEnumerableGlobalCleanupIsRejected()
        {
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableGlobalCleanupClass))).ToArrayAsync();

            Assert.Contains(validationErrors, v =>
                v.IsCritical && v.Message.Contains("[GlobalCleanupAttribute]") && v.Message.Contains("async enumerable"));
        }

        public class AsyncEnumerableIterationSetupClass
        {
            [IterationSetup]
            public async IAsyncEnumerable<int> Setup()
            {
                await Task.Yield();
                yield return 1;
            }

            [Benchmark]
            public void Benchmark() { }
        }

        [Fact]
        public async Task AsyncEnumerableIterationSetupIsRejected()
        {
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableIterationSetupClass))).ToArrayAsync();

            Assert.Contains(validationErrors, v =>
                v.IsCritical && v.Message.Contains("[IterationSetupAttribute]") && v.Message.Contains("async enumerable"));
        }

        public class AsyncEnumerableIterationCleanupClass
        {
            [IterationCleanup]
            public async IAsyncEnumerable<int> Cleanup()
            {
                await Task.Yield();
                yield return 1;
            }

            [Benchmark]
            public void Benchmark() { }
        }

        [Fact]
        public async Task AsyncEnumerableIterationCleanupIsRejected()
        {
            var validationErrors = await SetupCleanupValidator.FailOnError.ValidateAsync(
                BenchmarkConverter.TypeToBenchmarks(typeof(AsyncEnumerableIterationCleanupClass))).ToArrayAsync();

            Assert.Contains(validationErrors, v =>
                v.IsCritical && v.Message.Contains("[IterationCleanupAttribute]") && v.Message.Contains("async enumerable"));
        }
    }
}
