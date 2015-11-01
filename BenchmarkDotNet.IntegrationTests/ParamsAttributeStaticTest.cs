using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    // Delibrately made the Property "static" to ensure that ParametersSets also work okay in this scenario
    public class ParamsTestStaticProperty : IntegrationTestBase
    {
        public ParamsTestStaticProperty()
        {
            StaticParamProperty = BenchmarkState.Instance.IntParam;
        }

        public static int StaticParamProperty { get; set; }

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var reports = new BenchmarkRunner().Run<ParamsTestStaticProperty>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, GetTestOutput());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, GetTestOutput());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1, intParams: new[] { 1, 2, 3, 8, 9, 10 })]
        public void Benchmark()
        {
            if (collectedParams.Contains(StaticParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {StaticParamProperty} ###");
                collectedParams.Add(StaticParamProperty);
            }
        }
    }

    // Delibrately made everything "static" (as well as using a Field) to ensure that ParametersSets also work okay in this scenario
    public class ParamsTestStaticField : IntegrationTestBase
    {
        public ParamsTestStaticField()
        {
            StaticParamField = BenchmarkState.Instance.IntParam;
        }

        public static int StaticParamField = 0;

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public static void Test()
        {
            var reports = new BenchmarkRunner().Run<ParamsTestStaticField>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, GetTestOutput());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, GetTestOutput());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1, intParams: new[] { 1, 2, 3, 8, 9, 10 })]
        public static void Benchmark()
        {
            if (collectedParams.Contains(StaticParamField) == false)
            {
                Console.WriteLine($"// ### New Parameter {StaticParamField} ###");
                collectedParams.Add(StaticParamField);
            }
        }
    }
}
