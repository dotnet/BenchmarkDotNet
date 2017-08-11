using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GlobalSetupAttributeTargetTest : BenchmarkTestExecutor
    {
        private const string BaselineGlobalSetupCalled = "// ### Baseline GlobalSetup called ###";
        private const string BaselineBenchmarkCalled = "// ### Baseline Benchmark called ###";
        private const string FirstGlobalSetupCalled = "// ### First GlobalSetup called ###";
        private const string FirstBenchmarkCalled = "// ### First Benchmark called ###";
        private const string SecondGlobalSetupCalled = "// ### Second GlobalSetup called ###";
        private const string SecondBenchmarkCalled = "// ### Second Benchmark called ###";

        public GlobalSetupAttributeTargetTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalSetupTargetSpecificMethodTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<GlobalSetupAttributeTargetBenchmarks>(config);
            
            string log = logger.GetLog();

            Assert.Contains(BaselineGlobalSetupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(BaselineGlobalSetupCalled + Environment.NewLine) < 
                log.IndexOf(BaselineBenchmarkCalled + Environment.NewLine));

            Assert.Contains(FirstGlobalSetupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(FirstGlobalSetupCalled + Environment.NewLine) <
                log.IndexOf(FirstBenchmarkCalled + Environment.NewLine));

            Assert.Contains(SecondGlobalSetupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(SecondGlobalSetupCalled + Environment.NewLine) <
                log.IndexOf(SecondBenchmarkCalled + Environment.NewLine));
        }

        public class GlobalSetupAttributeTargetBenchmarks
        {
            private int setupValue;

            [GlobalSetup]
            public void BaselineSetup()
            {
                setupValue = -1;

                Console.WriteLine(BaselineGlobalSetupCalled);
            }

            [Benchmark(Baseline = true)]
            public void BaselineBenchmark()
            {
                Assert.Equal(-1, setupValue);

                Console.WriteLine(BaselineBenchmarkCalled);
            }

            [GlobalSetup(Target = nameof(Benchmark1))]
            public void GlobalSetup1()
            {
                setupValue = 1;

                Console.WriteLine(FirstGlobalSetupCalled);
            }

            [Benchmark]
            public void Benchmark1()
            {
                Assert.Equal(1, setupValue);

                Console.WriteLine(FirstBenchmarkCalled);
            }
            
            [GlobalSetup(Target = nameof(Benchmark2))]
            public void GlobalSetup2()
            {
                setupValue = 2;

                Console.WriteLine(SecondGlobalSetupCalled);
            }

            [Benchmark]
            public void Benchmark2()
            {
                Assert.Equal(2, setupValue);

                Console.WriteLine(SecondBenchmarkCalled);
            }
        }
    }
}
