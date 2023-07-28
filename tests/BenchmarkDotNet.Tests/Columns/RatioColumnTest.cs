using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Mocks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Columns
{
    public class RatioColumnTest
    {
        private readonly ITestOutputHelper output;

        public RatioColumnTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RatioColumnTest01()
        {
            var summary = MockRunner.Run<BenchmarkClass>(output, name => name switch
            {
                "Foo" => new double[] { 2, 2, 2 },
                "Bar" => new double[] { 4, 4, 4 },
                _ => throw new InvalidOperationException()
            });

            var ratioColumn = summary.GetColumns().FirstOrDefault(column => column.ColumnName == "Ratio");
            Assert.NotNull(ratioColumn);

            var fooCase = summary.BenchmarksCases.First(c => c.Descriptor.WorkloadMethod.Name == "Foo");
            var barCase = summary.BenchmarksCases.First(c => c.Descriptor.WorkloadMethod.Name == "Bar");
            Assert.Equal("1.00", ratioColumn.GetValue(summary, fooCase));
            Assert.Equal("2.00", ratioColumn.GetValue(summary, barCase));
        }

        public class BenchmarkClass
        {
            [Benchmark(Baseline = true)]
            public void Foo() { }

            [Benchmark]
            public void Bar() { }
        }
    }
}