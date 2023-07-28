using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Exporters
{
    internal class DescriptorNameDescriptionOverrideTests
    {
        public class MethodDescriptionOverrideTests
        {
            [Benchmark]
            public void VoidTest() { }

            [Benchmark(Description = "from Benchmark")]
            public void BenchmarkAttributeOverride()
            {
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("OverrideFromBenchmarkAttribute", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }

            [Benchmark]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void BenchmarkDescriptionAttributeOverride()
            {
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("OverrideFromBenchmarkDescriptionMethod", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }

            [Benchmark(Description = "Who are the winner?")]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void BothAttributeOverride()
            {
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("OverrideFromBothAttribute", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }
        }
        [BenchmarkDescription("FromClassDescription")]
        public class ClassDescriptionOverrideTests
        {
            [Benchmark]
            public void VoidTest() { }

            [Benchmark(Description = "from Benchmark")]
            public void ClassBenchmarkAttributeOverride(){
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("ClassOverrideFromBenchmarkAttribute", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }

            [Benchmark]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void ClassBenchmarkDescriptionAttributeOverride(){
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("ClassOverrideFromBenchmarkDescriptionMethod", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }

            [Benchmark(Description = "Who are the winner?")]
            [BenchmarkDescription("OverrideFromAttribute")]
            public void ClassBothAttributeOverride(){
                var description = BenchmarkConverter.TypeToBenchmarks(typeof(MethodDescriptionOverrideTests));

                Assert.Equal("ClassOverrideFromBothAttribute", description.BenchmarksCases[0].Descriptor.DisplayInfo);
            }
        }
    }
}
