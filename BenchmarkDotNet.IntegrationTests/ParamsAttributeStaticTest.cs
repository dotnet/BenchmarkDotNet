using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    // Delibrately made the Property "static" to ensure that Params also work okay in this scenario
    public class ParamsTestStaticProperty
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public static int StaticParamProperty { get; set; }

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = new BenchmarkPluginBuilder().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<ParamsTestStaticProperty>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
            if (collectedParams.Contains(StaticParamProperty) == false)
            {
                Console.WriteLine($"// ### New Parameter {StaticParamProperty} ###");
                collectedParams.Add(StaticParamProperty);
            }
        }
    }

    // Delibrately made everything "static" (as well as using a Field) to ensure that Params also work okay in this scenario
    public class ParamsTestStaticField
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public static int StaticParamField = 0;

        private static HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public static void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = new BenchmarkPluginBuilder().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<ParamsTestStaticField>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
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
