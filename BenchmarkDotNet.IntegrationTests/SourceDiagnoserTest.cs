#if !CORE
using BenchmarkDotNet.Diagnostics;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(ThroughputFastConfig))]
    public class SourceDiagnoserTest 
    {
        [Fact(Skip = "TODO")]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var sourceDiagnoser = new SourceDiagnoser();
            var config = DefaultConfig.Instance.With(logger).With(sourceDiagnoser);
            BenchmarkRunner.Run<SourceDiagnoserTest>(config);

            var testOutput = logger.GetLog();
            Assert.Contains($"Printing Code for Method: {this.GetType().FullName}.DictionaryEnumeration()", testOutput);
            Assert.Contains("PrintAssembly=True", testOutput);
        }

        [Benchmark]
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