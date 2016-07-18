using System;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValuesReturnedByBenchmarkTest : BenchmarkTestExecutor
    {
        public ValuesReturnedByBenchmarkTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AnyValueCanBeReturned()
        {
            CanExecute<ValuesReturnedByBenchmark>();
        }

        public class ValuesReturnedByBenchmark
        {
#if !CORE
            [Benchmark]
            public System.Windows.Point? TypeFromCustomFrameworkAssembly()
            {
                return new System.Windows.Point();
            }

            [Benchmark]
            public Diagnostics.Windows.InliningDiagnoser TypeFromCustomDependency()
            {
                return new Diagnostics.Windows.InliningDiagnoser();
            }
#endif

            [Benchmark]
            public object ReturnNullForReferenceType() => null;

            [Benchmark]
            public object ReturnNotNullForReferenceType() => new object();

            [Benchmark]
            public DateTime? ReturnNullForNullableType() => null;

            [Benchmark]
            public DateTime? ReturnNotNullForNullableType() => DateTime.UtcNow;

            [Benchmark]
            public DateTime ReturnDefaultValueForValueType() => default(DateTime);

            [Benchmark]
            public DateTime ReturnNonDefaultValueForValueType() => DateTime.UtcNow;
        }
    }
}