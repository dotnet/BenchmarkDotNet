using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Tests.Exporters
{
    internal class DescriptorNameDescriptionOverrideTests
    {
        public class MethodDescriptionOverrideTests
        {
            [Benchmark]
            public void VoidTest() { }

            [Benchmark(Description = "from Benchmark")]
            public void BenchmarkAttributeOverride() { }

            [Benchmark]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void BenchmarkDescriptionAttributeOverride() { }

            [Benchmark(Description = "Who are the winner?")]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void BothAttributeOverride() { }
        }
        [BenchmarkDescription("FromClassDescription")]
        public class ClassDescriptionOverrideTests
        {
            [Benchmark]
            public void VoidTest() { }

            [Benchmark(Description = "from Benchmark")]
            public void ClassBenchmarkAttributeOverride() { }

            [Benchmark]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void ClassBenchmarkDescriptionAttributeOverride() { }

            [Benchmark(Description = "Who are the winner?")]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void ClassBothAttributeOverride() { }
        }
    }
}
