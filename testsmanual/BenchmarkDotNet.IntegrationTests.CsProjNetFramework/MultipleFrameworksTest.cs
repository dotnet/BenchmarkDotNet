using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class MultipleFrameworksTest : BenchmarkTestExecutor
    {
        private const string Prefix = "// ### Called: ";
        private const string GlobalSetupCalled = Prefix + "GlobalSetup";
        private const string GlobalCleanupCalled = Prefix + "GlobalCleanup";
        private const string IterationSetupCalled = Prefix + "IterationSetup";
        private const string IterationCleanupCalled = Prefix + "IterationCleanup";

        private const string ExpectedBenchmarkNet461 = Prefix + "Benchmark NET461";
        private const string ExpectedBenchmarkNet48 = Prefix + "Benchmark NET48";
        private const string ExpectedBenchmarkNetCoreApp20 = Prefix + "Benchmark NETCOREAPP2_0";
        private const string ExpectedBenchmarkNet70 = Prefix + "Benchmark NET7_0";
#if NET461
        private const string BenchmarkCalled = ExpectedBenchmarkNet461;
#elif NET48
        private const string BenchmarkCalled = ExpectedBenchmarkNet48;
#elif NETCOREAPP2_0
        private const string BenchmarkCalled = ExpectedBenchmarkNetCoreApp20;
#elif NET7_0
        private const string BenchmarkCalled = ExpectedBenchmarkNet70;
#endif

        public MultipleFrameworksTest(ITestOutputHelper output) : base(output) { }

        private static string[] GetActualLogLines(Summary summary)
            => GetSingleStandardOutput(summary).Where(line => line.StartsWith(Prefix)).ToArray();

        [Theory]
        [InlineData(RuntimeMoniker.Net461)]
        [InlineData(RuntimeMoniker.Net48)]
        [InlineData(RuntimeMoniker.NetCoreApp20)]
        [InlineData(RuntimeMoniker.Net70)]
        public void EachFrameworkIsRebuilt(RuntimeMoniker runtime)
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob")
                .WithRuntime(runtime.GetRuntime());
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<SimpleBenchmark>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);

            string expectedBenchmarkCalled = runtime switch
            {
                RuntimeMoniker.Net461 => ExpectedBenchmarkNet461,
                RuntimeMoniker.Net48 => ExpectedBenchmarkNet48,
                RuntimeMoniker.NetCoreApp20 => ExpectedBenchmarkNetCoreApp20,
                RuntimeMoniker.Net70 => ExpectedBenchmarkNet70,
                _ => throw new ArgumentException("Unexpected runtime: " + runtime)
            };

            string[] expectedLogLines =
            {
                "// ### Called: GlobalSetup",

                "// ### Called: IterationSetup (1)", // MainWarmup1
                expectedBenchmarkCalled, // MainWarmup1
                "// ### Called: IterationCleanup (1)", // MainWarmup1
                "// ### Called: IterationSetup (2)", // MainWarmup2
                expectedBenchmarkCalled, // MainWarmup2
                "// ### Called: IterationCleanup (2)", // MainWarmup2

                "// ### Called: IterationSetup (3)", // MainTarget1
                expectedBenchmarkCalled, // MainTarget1
                "// ### Called: IterationCleanup (3)", // MainTarget1
                "// ### Called: IterationSetup (4)", // MainTarget2
                expectedBenchmarkCalled, // MainTarget2
                "// ### Called: IterationCleanup (4)", // MainTarget2
                "// ### Called: IterationSetup (5)", // MainTarget3
                expectedBenchmarkCalled, // MainTarget3
                "// ### Called: IterationCleanup (5)", // MainTarget3

                "// ### Called: GlobalCleanup"
            };

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