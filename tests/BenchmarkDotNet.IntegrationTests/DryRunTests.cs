using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DryRunTests : BenchmarkTestExecutor
    {
        public DryRunTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WelchTTest() => CanExecute<Bench>(CreateSimpleConfig());

        [DryJob, WelchTTestPValueColumn]
        public class Bench
        {
            [Benchmark(Baseline = true)]
            public void A() => Thread.Sleep(10);

            [Benchmark]
            public void B() => Thread.Sleep(10);
        }
    }
}