using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Configs
{
    public class CategoriesTests
    {
        private readonly ITestOutputHelper output;

        public CategoriesTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CategoryInheritanceTest()
        {
            string Format(BenchmarkCase benchmarkCase) =>
                benchmarkCase.Descriptor.WorkloadMethod.Name + ": " +
                string.Join("+", benchmarkCase.Descriptor.Categories.OrderBy(category => category));

            var benchmarkCases = BenchmarkConverter
                .TypeToBenchmarks(typeof(DerivedClass))
                .BenchmarksCases
                .OrderBy(x => x.Descriptor.WorkloadMethod.Name)
                .ToList();
            Assert.Equal(2, benchmarkCases.Count);

            output.WriteLine(Format(benchmarkCases[0]));
            output.WriteLine(Format(benchmarkCases[1]));

            Assert.Equal("BaseMethod: BaseClassCategory+BaseMethodCategory+DerivedClassCategory", Format(benchmarkCases[0]));
            Assert.Equal("DerivedMethod: BaseClassCategory+DerivedClassCategory+DerivedMethodCategory", Format(benchmarkCases[1]));
        }

        [BenchmarkCategory("BaseClassCategory")]
        public class BaseClass
        {
            [Benchmark]
            [BenchmarkCategory("BaseMethodCategory")]
            public void BaseMethod() { }
        }

        [BenchmarkCategory("DerivedClassCategory")]
        public class DerivedClass : BaseClass
        {
            [Benchmark]
            [BenchmarkCategory("DerivedMethodCategory")]
            public void DerivedMethod() { }
        }
    }
}