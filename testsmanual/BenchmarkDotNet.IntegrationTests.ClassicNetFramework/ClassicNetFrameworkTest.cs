using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ClassicNetFrameworkTest : BenchmarkTestExecutor
    {
        private const string Prefix = "// ### Called: ";
        private const string GlobalSetupCalled = Prefix + "GlobalSetup";
        private const string GlobalCleanupCalled = Prefix + "GlobalCleanup";
        private const string IterationSetupCalled = Prefix + "IterationSetup";
        private const string IterationCleanupCalled = Prefix + "IterationCleanup";
        private const string BenchmarkCalled = Prefix + "Benchmark";

        private readonly string[] expectedLogLines = {
            "// ### Called: GlobalSetup",

            "// ### Called: IterationSetup (1)", // MainWarmup1
            "// ### Called: Benchmark", // MainWarmup1
            "// ### Called: IterationCleanup (1)", // MainWarmup1
            "// ### Called: IterationSetup (2)", // MainWarmup2
            "// ### Called: Benchmark", // MainWarmup2
            "// ### Called: IterationCleanup (2)", // MainWarmup2

            "// ### Called: IterationSetup (3)", // MainTarget1
            "// ### Called: Benchmark", // MainTarget1
            "// ### Called: IterationCleanup (3)", // MainTarget1
            "// ### Called: IterationSetup (4)", // MainTarget2
            "// ### Called: Benchmark", // MainTarget2
            "// ### Called: IterationCleanup (4)", // MainTarget2
            "// ### Called: IterationSetup (5)", // MainTarget3
            "// ### Called: Benchmark", // MainTarget3
            "// ### Called: IterationCleanup (5)", // MainTarget3

            "// ### Called: GlobalCleanup"
        };

        public ClassicNetFrameworkTest(ITestOutputHelper output) : base(output) { }

        private static string[] GetActualLogLines(Summary summary)
            => GetSingleStandardOutput(summary).Where(line => line.StartsWith(Prefix)).ToArray();

        [Fact]
        public void ClassicNetFrameworkProjectWorks()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<SimpleBenchmark>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            SmartAssert.Equal(expectedLogLines, actualLogLines);
        }

        public class SimpleBenchmark
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