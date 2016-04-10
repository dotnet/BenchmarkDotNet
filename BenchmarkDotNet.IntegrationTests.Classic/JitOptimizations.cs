using System;
using System.Linq;
using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class JitOptimizations
    {
        [Fact]
        public void UserGetsWarningWhenNonOptimizedDllIsReferenced()
        {
            var summaryWithNonOptimizedDll = CreateSummary(typeof(DisabledOptimizations.OptimizationsDisabledInCsproj));

            var warnings = JitOptimizationsAnalyser.Instance.Analyze(summaryWithNonOptimizedDll).ToArray();

            Assert.NotEmpty(warnings);
        }

        [Fact]
        public void UserGetsNoWarningWhenOnlyOptimizedDllAreReferenced()
        {
            var summaryWithOptimizedDll = CreateSummary(typeof(EnabledOptimizations.OptimizationsEnabledInCsproj));

            var warnings = JitOptimizationsAnalyser.Instance.Analyze(summaryWithOptimizedDll).ToArray();

            Assert.Empty(warnings);
        }

        private Summary CreateSummary(Type targetBenchmarkType)
        {
            return new Summary(
                string.Empty,
                new[]
                {
                    new BenchmarkReport(
                        new Benchmark(
                            new Target(targetBenchmarkType, null),
                            Jobs.Job.Dry,
                            new ParameterInstances(new [] { new ParameterInstance(new ParameterDefinition("nothing", false, new object[0]), "some") } )),
                        null,
                        null,
                        null,
                        null)
                },
                EnvironmentInfo.GetCurrent(),
                new Configs.ManualConfig(),
                null,
                TimeSpan.Zero);
        }
    }
}