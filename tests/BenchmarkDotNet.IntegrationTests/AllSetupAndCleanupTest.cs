using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AllSetupAndCleanupTest : BenchmarkTestExecutor
    {
        private const string Prefix = "// ### Called: ";
        private const string GlobalSetupCalled = Prefix + "GlobalSetup";
        private const string GlobalCleanupCalled = Prefix + "GlobalCleanup";
        private const string IterationSetupCalled = Prefix + "IterationSetup";
        private const string IterationCleanupCalled = Prefix + "IterationCleanup";
        private const string BenchmarkCalled = Prefix + "Benchmark";
        private const string OutputDelimeter = "===========================================================";

        private readonly string[] expectedLogLines = {
            "// ### Called: GlobalSetup",
            
            "// ### Called: IterationSetup (1)", // IterationSetup Jitting
            "// ### Called: IterationCleanup (1)", // IterationCleanup Jitting
            
            "// ### Called: IterationSetup (2)", // MainWarmup1
            "// ### Called: Benchmark", // MainWarmup1
            "// ### Called: IterationCleanup (2)", // MainWarmup1
            "// ### Called: IterationSetup (3)", // MainWarmup2
            "// ### Called: Benchmark", // MainWarmup2
            "// ### Called: IterationCleanup (3)", // MainWarmup2
            
            "// ### Called: IterationSetup (4)", // MainTarget1
            "// ### Called: Benchmark", // MainTarget1
            "// ### Called: IterationCleanup (4)", // MainTarget1
            "// ### Called: IterationSetup (5)", // MainTarget2
            "// ### Called: Benchmark", // MainTarget2
            "// ### Called: IterationCleanup (5)", // MainTarget2
            "// ### Called: IterationSetup (6)", // MainTarget3
            "// ### Called: Benchmark", // MainTarget3
            "// ### Called: IterationCleanup (6)", // MainTarget3
            
            "// ### Called: GlobalCleanup"
        };

        public AllSetupAndCleanupTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AllSetupAndCleanupMethodRunsTest()
        {
            var logger = new OutputLogger(Output);
            var miniJob = Job.Default.With(RunStrategy.Monitoring).WithWarmupCount(2).WithTargetCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(logger, miniJob);

            CanExecute<AllSetupAndCleanupAttributeBenchmarks>(config);
            Output.WriteLine(OutputDelimeter);
            Output.WriteLine(OutputDelimeter);
            Output.WriteLine(OutputDelimeter);
            
            var actualLogLines = logger.GetLog().Split('\r', '\n').Where(line => line.StartsWith(Prefix)).ToArray();
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarks
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public void GlobalSetup() => Console.WriteLine(GlobalSetupCalled);

            [GlobalCleanup]
            public void GlobalCleanup() => Console.WriteLine(GlobalCleanupCalled);

            [Benchmark]
            public void Benchmark() => Console.WriteLine(BenchmarkCalled);
        }
    }
}