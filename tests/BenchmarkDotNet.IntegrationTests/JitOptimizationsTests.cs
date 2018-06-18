using System;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class JitOptimizationsTests
    {
        private readonly ITestOutputHelper output;

        public JitOptimizationsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

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

            if (warnings.Any())
            {
                output.WriteLine("*** Warnings ***");
                foreach (var warning in warnings)
                    output.WriteLine(warning.Message);
            }

#if DEBUG
            Assert.NotEmpty(warnings);
#else
            Assert.Empty(warnings);
#endif
        }

        private BenchmarkCase[] CreateBenchmarks(Type targetBenchmarkType)
        {
            return BenchmarkConverter.TypeToBenchmarks(targetBenchmarkType).BenchmarksCases;
        }
    }
}