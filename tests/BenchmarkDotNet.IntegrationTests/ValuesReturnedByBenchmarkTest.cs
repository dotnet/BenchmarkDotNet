using System;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ValuesReturnedByBenchmarkTest
    {
        [Fact]
        public void AnyValueCanBeReturned()
        {
            BenchmarkTestExecutor.CanExecute<ValuesReturnedByBenchmarkTest>();
        }

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