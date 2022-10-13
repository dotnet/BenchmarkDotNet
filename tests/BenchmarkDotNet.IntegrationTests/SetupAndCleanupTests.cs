using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Tests.Loggers;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAndCleanupTests : BenchmarkTestExecutor
    {
        private const string FirstPrefix = "// ### First Called: ";
        private const string FirstGlobalSetupCalled = FirstPrefix + "GlobalSetup";
        private const string FirstGlobalCleanupCalled = FirstPrefix + "GlobalCleanup";
        private const string FirstIterationSetupCalled = FirstPrefix + "IterationSetup";
        private const string FirstIterationCleanupCalled = FirstPrefix + "IterationCleanup";
        private const string FirstBenchmarkCalled = FirstPrefix + "Benchmark";

        private const string SecondPrefix = "// ### Second Called: ";
        private const string SecondGlobalSetupCalled = SecondPrefix + "GlobalSetup";
        private const string SecondGlobalCleanupCalled = SecondPrefix + "GlobalCleanup";
        private const string SecondIterationSetupCalled = SecondPrefix + "IterationSetup";
        private const string SecondIterationCleanupCalled = SecondPrefix + "IterationCleanup";
        private const string SecondBenchmarkCalled = SecondPrefix + "Benchmark";

        private const string OutputDelimiter = "===========================================================";

        private readonly string[] firstExpectedLogLines = {
            "// ### First Called: GlobalSetup",

            "// ### First Called: IterationSetup (1)", // MainWarmup1
            "// ### First Called: Benchmark", // MainWarmup1
            "// ### First Called: IterationCleanup (1)", // MainWarmup1
            "// ### First Called: IterationSetup (2)", // MainWarmup2
            "// ### First Called: Benchmark", // MainWarmup2
            "// ### First Called: IterationCleanup (2)", // MainWarmup2

            "// ### First Called: IterationSetup (3)", // MainTarget1
            "// ### First Called: Benchmark", // MainTarget1
            "// ### First Called: IterationCleanup (3)", // MainTarget1
            "// ### First Called: IterationSetup (4)", // MainTarget2
            "// ### First Called: Benchmark", // MainTarget2
            "// ### First Called: IterationCleanup (4)", // MainTarget2
            "// ### First Called: IterationSetup (5)", // MainTarget3
            "// ### First Called: Benchmark", // MainTarget3
            "// ### First Called: IterationCleanup (5)", // MainTarget3

            "// ### First Called: GlobalCleanup"
        };

        private readonly string[] secondExpectedLogLines = {
            "// ### Second Called: GlobalSetup",

            "// ### Second Called: IterationSetup (1)", // MainWarmup1
            "// ### Second Called: Benchmark", // MainWarmup1
            "// ### Second Called: IterationCleanup (1)", // MainWarmup1
            "// ### Second Called: IterationSetup (2)", // MainWarmup2
            "// ### Second Called: Benchmark", // MainWarmup2
            "// ### Second Called: IterationCleanup (2)", // MainWarmup2

            "// ### Second Called: IterationSetup (3)", // MainTarget1
            "// ### Second Called: Benchmark", // MainTarget1
            "// ### Second Called: IterationCleanup (3)", // MainTarget1
            "// ### Second Called: IterationSetup (4)", // MainTarget2
            "// ### Second Called: Benchmark", // MainTarget2
            "// ### Second Called: IterationCleanup (4)", // MainTarget2
            "// ### Second Called: IterationSetup (5)", // MainTarget3
            "// ### Second Called: Benchmark", // MainTarget3
            "// ### Second Called: IterationCleanup (5)", // MainTarget3

            "// ### Second Called: GlobalCleanup"
        };

        public SetupAndCleanupTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AllSetupAndCleanupMethodRunsForSpecificBenchmark()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<Benchmarks>(config);
            var standardOutput = GetCombinedStandardOutput(summary);
            Output.WriteLine(OutputDelimiter);
            Output.WriteLine(OutputDelimiter);
            Output.WriteLine(OutputDelimiter);

            var firstActualLogLines = standardOutput.Where(line => line.StartsWith(FirstPrefix)).ToArray();
            foreach (string line in firstActualLogLines)
                Output.WriteLine(line);
            Assert.Equal(firstExpectedLogLines, firstActualLogLines);

            var secondActualLogLines = standardOutput.Where(line => line.StartsWith(SecondPrefix)).ToArray();
            foreach (string line in secondActualLogLines)
                Output.WriteLine(line);
            Assert.Equal(secondExpectedLogLines, secondActualLogLines);
        }

        public class Benchmarks
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup(Target = nameof(FirstBenchmark))]
            public void FirstIterationSetup() => Console.WriteLine(FirstIterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup(Target = nameof(FirstBenchmark))]
            public void FirstIterationCleanup() => Console.WriteLine(FirstIterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup(Target = nameof(FirstBenchmark))]
            public void FirstGlobalSetup() => Console.WriteLine(FirstGlobalSetupCalled);

            [GlobalCleanup(Target = nameof(FirstBenchmark))]
            public void FirstGlobalCleanup() => Console.WriteLine(FirstGlobalCleanupCalled);

            [Benchmark]
            public void FirstBenchmark() => Console.WriteLine(FirstBenchmarkCalled);


            [IterationSetup(Target = nameof(SecondBenchmark))]
            public void SecondIterationSetup() => Console.WriteLine(SecondIterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup(Target = nameof(SecondBenchmark))]
            public void SecondIterationCleanup() => Console.WriteLine(SecondIterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup(Target = nameof(SecondBenchmark))]
            public void SecondGlobalSetup() => Console.WriteLine(SecondGlobalSetupCalled);

            [GlobalCleanup(Target = nameof(SecondBenchmark))]
            public void SecondGlobalCleanup() => Console.WriteLine(SecondGlobalCleanupCalled);

            [Benchmark]
            public void SecondBenchmark() => Console.WriteLine(SecondBenchmarkCalled);
        }
    }
}
