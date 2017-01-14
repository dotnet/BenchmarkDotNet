using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class DryRunTests : BenchmarkTestExecutor
    {
        public DryRunTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WelchTTest() => CanExecute<WelchTTestBench>(CreateSimpleConfig());

        [DryJob, WelchTTestPValueColumn]
        public class WelchTTestBench
        {
            [Benchmark(Baseline = true)]
            public void A() => Thread.Sleep(10);

            [Benchmark]
            public void B() => Thread.Sleep(10);
        }

        private const string CounterPrefix = "// Counter = ";

        [Fact]
        public void ColdStart()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<ColdStartBench>(config);

            string log = logger.GetLog();
            Assert.Contains($"{CounterPrefix}1", log);
            Assert.DoesNotContain($"{CounterPrefix}2", log);
        }

        [DryJob]
        public class ColdStartBench
        {
            private int counter;

            [Benchmark]
            public void Foo()
            {
                counter++;
                Console.WriteLine($"{CounterPrefix}{counter}");
            }
        }
    }
}