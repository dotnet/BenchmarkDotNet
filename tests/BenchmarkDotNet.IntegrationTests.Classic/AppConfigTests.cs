using System;
using System.Configuration;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class AppConfigTests
    {
        private readonly ITestOutputHelper output;

        public AppConfigTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void CustomSettingsGetRewritten()
        {
            BenchmarkTestRunner.CanCompileAndRun<AppConfigConsumingBenchmark>(output);
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