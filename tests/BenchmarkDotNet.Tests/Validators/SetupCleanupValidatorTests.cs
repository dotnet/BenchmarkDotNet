using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

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
        public void InvalidGlobalSetupTooManyBlankTargets()
        {
            var validationErrors = SetupCleanupValidator.FailOnError.Validate(
                BenchmarkConverter.TypeToBenchmarks(typeof(BlankTargetClass))).ToArray();

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
        public void InvalidGlobalSetupTooManyExplicitTargets()
        {
            var validationErrors = SetupCleanupValidator.FailOnError.Validate(
                BenchmarkConverter.TypeToBenchmarks(typeof(ExplicitTargetClass))).ToArray();

            var count = validationErrors.Count(v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("Target = Benchmark"));

            Assert.Equal(1, count);
        }
    }
}
