using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class JitOptimizations
    {
        [Fact]
        public void UserGetsWarningWhenNonOptimizedDllIsReferenced()
        {
            var benchmarksWithNonOptimizedDll = CreateBenchmarks(typeof(DisabledOptimizations.OptimizationsDisabledInCsproj));

            var warnings = JitOptimizationsValidator.DontFailOnError.Validate(benchmarksWithNonOptimizedDll).ToArray();
            var criticalErrors = JitOptimizationsValidator.FailOnError.Validate(benchmarksWithNonOptimizedDll).ToArray();

            Assert.NotEmpty(warnings);
            Assert.True(warnings.All(error => error.IsCritical == false));
            Assert.NotEmpty(criticalErrors);
            Assert.True(criticalErrors.All(error => error.IsCritical));
        }

        [Fact]
        public void UserGetsNoWarningWhenOnlyOptimizedDllAreReferenced()
        {
            var benchmarksWithOptimizedDll = CreateBenchmarks(typeof(EnabledOptimizations.OptimizationsEnabledInCsproj));

            var warnings = JitOptimizationsValidator.DontFailOnError.Validate(benchmarksWithOptimizedDll).ToArray();

            Assert.Empty(warnings);
        }

        private IList<Benchmark> CreateBenchmarks(Type targetBenchmarkType)
        {
            return BenchmarkConverter.TypeToBenchmarks(targetBenchmarkType);
        }
    }
}