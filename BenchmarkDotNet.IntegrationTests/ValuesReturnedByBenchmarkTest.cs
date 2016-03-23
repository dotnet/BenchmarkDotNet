using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(SingleRunFastConfig))]
    public class ValuesReturnedByBenchmarkTest
    {
        [Fact]
        public void AnyValueCanBeReturned()
        {
            var summary = BenchmarkRunner.Run<ValuesReturnedByBenchmarkTest>();

            Assert.True(summary.Reports.Any());
            Assert.True(summary.Reports.All(report => report.Value.ExecuteResults.All(executeResult => executeResult.FoundExecutable)));
            Assert.True(summary.Reports.All(report => report.Value.AllMeasurements.Any()));
        }

#if !CORE
        [Benchmark]
        public System.Windows.Point? TypeFromCustomFrameworkAssembly()
        {
            return new System.Windows.Point();
        }

        [Benchmark]
        public Diagnostics.InliningDiagnoser TypeFromCustomDependency()
        {
            return new Diagnostics.InliningDiagnoser();
        }
#endif

        [Benchmark]
        public object ReturnNullForReferenceType()
        {
            return null;
        }

        [Benchmark]
        public object ReturnNotNullForReferenceType()
        {
            return new object();
        }

        [Benchmark]
        public DateTime? ReturnNullForNullableType()
        {
            return null;
        }

        [Benchmark]
        public DateTime? ReturnNotNullForNullableType()
        {
            return DateTime.UtcNow;
        }

        [Benchmark]
        public DateTime ReturnDefaultValueForValueType()
        {
            return default(DateTime);
        }

        [Benchmark]
        public DateTime ReturnNonDefaultValueForValueType()
        {
            return DateTime.UtcNow;
        }
    }
}