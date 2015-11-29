using BenchmarkDotNet.Tasks;
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ParamsTestProperty
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public int ParamProperty { get; set; }

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<ParamsTestProperty>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {default(int)} ###" + Environment.NewLine, logger.GetLog());
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

    public class ParamsTestField
    {
        [Params(1, 2, 3, 8, 9, 10)]
        public int ParamField = 0;

        private HashSet<int> collectedParams = new HashSet<int>();

        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<ParamsTestField>();
            foreach (var param in new[] { 1, 2, 3, 8, 9, 10 })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter 0 ###" + Environment.NewLine, logger.GetLog());
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
