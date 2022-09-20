using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class PriorityTests : BenchmarkTestExecutor
    {
        public PriorityTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParamsSupportPropertyWithPublicSetter()
        {
            var config = CreateSimpleConfig();

            var summary = CanExecute<PriorityBenchmark>(config);
            var columns = summary.Table.Columns;
            var aColumn = columns.First(col => col.Header == "A");
            var bColumn = columns.First(col => col.Header == "b");
            var cColumn = columns.First(col => col.Header == "c");
            var dColumn = columns.First(col => col.Header == "d");
            var eColumn = columns.First(col => col.Header == "E");
            var fColumn = columns.First(col => col.Header == "F");

            Assert.NotNull(aColumn);
            Assert.NotNull(bColumn);
            Assert.NotNull(cColumn);
            Assert.NotNull(dColumn);
            Assert.NotNull(eColumn);
            Assert.NotNull(fColumn);

            Assert.True(aColumn.OriginalColumn.PriorityInCategory == -100);
            Assert.True(bColumn.OriginalColumn.PriorityInCategory == -10);
            Assert.True(cColumn.OriginalColumn.PriorityInCategory == 0);
            Assert.True(dColumn.OriginalColumn.PriorityInCategory == 0);
            Assert.True(eColumn.OriginalColumn.PriorityInCategory == 10);
            Assert.True(fColumn.OriginalColumn.PriorityInCategory == 50);

            Assert.True(aColumn.Index < bColumn.Index);
            Assert.True(bColumn.Index < cColumn.Index);
            Assert.True(cColumn.Index < dColumn.Index);
            Assert.True(dColumn.Index < eColumn.Index);
            Assert.True(eColumn.Index < fColumn.Index);
        }

        public class PriorityBenchmark
        {
            [Params(100, Priority = -100)]
            public int A { get; set; }

            [ParamsSource(nameof(NumberParams), Priority = 50)]
            public int F { get; set; }

            [ParamsAllValues(Priority = 10)]
            public bool E;

            [Arguments(5, Priority = -10)]
            [Arguments(10)]
            [Arguments(20)]
            [Benchmark]
            public int OneArgument(int b) => E ? A + b : F;

            [Benchmark]
            [ArgumentsSource(nameof(NumberArguments))]
            public int ManyArguments(int c, int d) => E ? A + c + d : F;

            public IEnumerable<object[]> NumberArguments()
            {
                yield return new object[] { 1, 2 };
            }

            public IEnumerable<int> NumberParams => new int[]
            {
                50
            };
        }
    }
}