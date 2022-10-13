using System.Globalization;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.Roslyn;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class RoslynToolchainTest : BenchmarkTestExecutor
    {
        public RoslynToolchainTest(ITestOutputHelper output) : base(output) { }

        /// <summary>Prooftest for #1039.</summary>
        [Tests.XUnit.TheoryFullFrameworkOnly("Roslyn toolchain does not support .NET Core")]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        [InlineData("ru-RU")]
        [InlineData("ja-JP")]
        public void CanExecuteWithNonDefaultUiCulture(string culture)
        {
            var originCulture = CultureInfo.CurrentCulture;
            var originUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var overrideCulture = CultureInfo.GetCultureInfo(culture);
                Assert.NotNull(overrideCulture);
                Assert.False(overrideCulture.IsNeutralCulture);
                CultureInfo.CurrentCulture = overrideCulture;
                CultureInfo.CurrentUICulture = overrideCulture;

                var miniJob = Job.Dry.WithToolchain(RoslynToolchain.Instance);
                var config = CreateSimpleConfig(job: miniJob);

                CanExecute<SimpleBenchmarks>(config);
            }
            finally
            {
                CultureInfo.CurrentCulture = originCulture;
                CultureInfo.CurrentUICulture = originUiCulture;
            }
        }

        public class SimpleBenchmarks
        {
            [Benchmark]
            public void Benchmark() => Thread.Sleep(5);
        }
    }
}
