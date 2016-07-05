using System;
using System.Configuration;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class AppConfigTests
    {
        [Fact]
        public void CustomSettingsGetRewritten()
        {
            BenchmarkTestRunner.CanCompileAndRun<AppConfigConsumingBenchmark>();
        }
    }

    public class AppConfigConsumingBenchmark
    {
        [Benchmark]
        public void ReadFromAppConfig()
        {
            if (ConfigurationManager.AppSettings["settings"] != "getsCopied")
            {
                throw new InvalidOperationException("The app config did not get copied");
            }
        }
    }
}