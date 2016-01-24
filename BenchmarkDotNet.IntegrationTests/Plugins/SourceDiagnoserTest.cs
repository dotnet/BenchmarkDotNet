#if !DNX451
using BenchmarkDotNet.Diagnostics;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using System.Collections.Generic;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Plugins
{
    public class SourceDiagnoserTest 
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var sourceDiagnoser = new BenchmarkSourceDiagnoser();
            var plugins = BenchmarkPluginBuilder.CreateDefault()
                                .AddLogger(logger)
                                .AddDiagnoser(sourceDiagnoser)
                                .Build();
            var reports = new BenchmarkRunner(plugins).Run<SourceDiagnoserTest>();

            var testOutput = logger.GetLog();
            Assert.Contains($"Printing Code for Method: {this.GetType().FullName}.DictionaryEnumeration()", testOutput);
            Assert.Contains("PrintAssembly=True", testOutput);
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.Throughput, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public Dictionary<string, string> DictionaryEnumeration()
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var item in dictionary)
            {
                ;
            }
            return dictionary;
        }
    }
}
#endif