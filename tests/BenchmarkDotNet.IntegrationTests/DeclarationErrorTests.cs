using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DeclarationErrorTests
    {
        [Fact]
        public void ARunReportsEveryBadDeclarationAtOnce()
        {
            var summary = Run(typeof(SeveralBadDeclarations), out string log);

            Assert.True(summary.HasCriticalValidationErrors);

            foreach (string expected in new[]
            {
                "GlobalSetup method Setup has incorrect access modifiers.",
                "Benchmark method NonPublic has incorrect access modifiers.",
                "Benchmark method Generic is generic.",
                "Benchmark method TakesAnArgument has incorrect signature."
            })
            {
                Assert.Contains(summary.ValidationErrors, error => error.Message.StartsWith(expected));
                Assert.Contains(expected, log);
            }
        }

#pragma warning disable BDN1103, BDN1104, BDN1400
        public class SeveralBadDeclarations
        {
            [GlobalSetup] private void Setup() { }

            [Benchmark] private void NonPublic() { }
            [Benchmark] public void Generic<T>() { }
            [Benchmark] public void TakesAnArgument(int x) { }
        }
#pragma warning restore BDN1103, BDN1104, BDN1400

        [Fact]
        public void TheReasonOutlivesATypeLeftWithNoCases()
        {
            var summary = Run(typeof(OnlyBenchmarkIsNonPublic), out _);

            Assert.Contains(summary.ValidationErrors, error => error.Message.StartsWith("Benchmark method Run has incorrect access modifiers."));
            Assert.DoesNotContain(summary.ValidationErrors, error => error.Message.Contains("No [Benchmark] attribute found"));
        }

#pragma warning disable BDN1103
        public class OnlyBenchmarkIsNonPublic
        {
            [Benchmark] private void Run() { }
        }
#pragma warning restore BDN1103

        [Fact]
        public void TheReasonSurvivesTheCommandLinePath()
        {
            var logger = new AccumulationLogger();

            var summaries = BenchmarkSwitcher
                .FromTypes([typeof(OnlyBenchmarkIsGeneric)])
                .Run(["--filter", "*"], ManualConfig.CreateEmpty().AddLogger(logger).AddColumnProvider(DefaultColumnProviders.Instance));

            Assert.True(summaries.Single().HasCriticalValidationErrors);
            Assert.Contains("Benchmark method Run is generic.", logger.GetLog());
        }

#pragma warning disable BDN1104
        public class OnlyBenchmarkIsGeneric
        {
            [Benchmark] public void Run<T>() { }
        }
#pragma warning restore BDN1104

        [Fact]
        public void DiscoveryAndTheValidatorBothReportWhatTheyKnow()
        {
            var summary = Run(typeof(BothHalvesRefuse), out _);

            Assert.Contains(summary.ValidationErrors, error => error.Message.StartsWith("Values of type BothHalvesRefuse returned null"));
            Assert.Contains(summary.ValidationErrors, error => error.Message.StartsWith("Unable to use BothHalvesRefuse.Value with [ParamsSource(Values)]"));
        }

#pragma warning disable BDN1306
        public class BothHalvesRefuse
        {
            [ParamsSource(nameof(Values))] public int Value { get; set; }

            public static object Values => null!;

            [Benchmark] public void Run() { }
        }
#pragma warning restore BDN1306

        private static Reports.Summary Run(Type type, out string log)
        {
            var logger = new AccumulationLogger();
            var summary = BenchmarkRunner.Run(type, ManualConfig.CreateEmpty().AddLogger(logger).AddColumnProvider(DefaultColumnProviders.Instance));

            log = logger.GetLog();
            return summary;
        }
    }
}
