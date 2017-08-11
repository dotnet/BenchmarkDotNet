using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupCleanupValidatorTests : BenchmarkTestExecutor
    {
        public SetupCleanupValidatorTests(ITestOutputHelper output) : base(output) { }

        #region Too Many Blank Targets

        public class BlankTargetClass
        {
            [GlobalSetup]
            public void SetupA()
            {

            }

            [GlobalSetup]
            public void SetupB()
            {

            }

            [Benchmark]
            public void Benchmark()
            {

            }
        }

        [Fact]
        public void InvalidGlobalSetupTooManyBlankTargets()
        {
            var results = CanExecute<BlankTargetClass>(fullValidation: false);

            Assert.True(results.HasCriticalValidationErrors);

            var count = results.ValidationErrors.Count(v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("Blank"));

            Assert.Equal(1, count);
        }

        #endregion

        #region Too Many Targets

        public class ExplicitTargetClass
        {
            [GlobalSetup(Target = nameof(Benchmark))]
            public void SetupA()
            {

            }

            [GlobalSetup(Target = nameof(Benchmark))]
            public void SetupB()
            {

            }

            [Benchmark]
            public void Benchmark()
            {

            }
        }

        [Fact]
        public void InvalidGlobalSetupTooManyExplicitTargets()
        {
            var results = CanExecute<ExplicitTargetClass>(fullValidation: false);

            Assert.True(results.HasCriticalValidationErrors);

            var count = results.ValidationErrors.Count(v =>
                v.IsCritical && v.Message.Contains("[GlobalSetupAttribute]") && v.Message.Contains("Target = Benchmark"));

            Assert.Equal(1, count);
        }

        #endregion
    }
}
