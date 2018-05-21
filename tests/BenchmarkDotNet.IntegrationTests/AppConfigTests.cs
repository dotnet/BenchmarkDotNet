using System;
using System.Configuration;
using BenchmarkDotNet.Attributes;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AppConfigTests : BenchmarkTestExecutor
    {
        public AppConfigTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CustomSettingsGetRewritten()
            => CanExecute<AppConfigConsumingBenchmark>();
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