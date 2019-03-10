using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.Tests.Configs
{
    public class ConfigPerMethodTests
    {
        [Fact]
        public void PetMethodConfigsAreRespected()
        {
            var never = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkThatShouldNeverRun));

            Assert.Empty(never.BenchmarksCases);

            var always = BenchmarkConverter.TypeToBenchmarks(typeof(WithBenchmarkThatShouldAlwaysRun));

            Assert.NotEmpty(always.BenchmarksCases);
        }

        public class WithBenchmarkThatShouldNeverRun
        {
            [Benchmark]
            [ConditionalRun(false)]
            public void Method() { }
        }

        public class WithBenchmarkThatShouldAlwaysRun
        {
            [Benchmark]
            [ConditionalRun(true)]
            public void Method() { }
        }

        public class ConditionalRun : FilterConfigBaseAttribute
        {
            public ConditionalRun(bool value) : base(new SimpleFilter(_ => value)) { }
        }
    }
}