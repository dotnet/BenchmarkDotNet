using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsTestProperty : IntegrationTestBase
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public int ParamProperty { get; set; }

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var reports = new BenchmarkRunner().RunCompetition(new ParamsTestProperty());
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" +  Environment.NewLine, GetTestOutput());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, GetTestOutput());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamProperty} ###");
                collectedParams.Add(ParamProperty);
            }
        }
    }

    public class ParamsTestField : IntegrationTestBase
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public int ParamField = 0;

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var reports = new BenchmarkRunner().RunCompetition(new ParamsTestField());
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, GetTestOutput());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, GetTestOutput());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
            if (collectedParams.Contains(ParamField) == false)
            {
                Console.WriteLine($"// ### New Parameter {ParamField} ###");
                collectedParams.Add(ParamField);
            }
        }
    }
}
