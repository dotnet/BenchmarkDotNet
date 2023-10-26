using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
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

        public AllSetupAndCleanupTest(ITestOutputHelper output) : base(output) { }

        private static string[] GetActualLogLines(Summary summary)
            => GetSingleStandardOutput(summary).Where(line => line.StartsWith(Prefix)).ToArray();

        [Fact]
        public void AllSetupAndCleanupMethodRunsTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarks>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            SmartAssert.Equal(expectedLogLines, actualLogLines);
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

        [Fact]
        public void AllSetupAndCleanupMethodRunsAsyncTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarksAsync>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarksAsync
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public Task GlobalSetup() => Console.Out.WriteLineAsync(GlobalSetupCalled);

            [GlobalCleanup]
            public Task GlobalCleanup() => Console.Out.WriteLineAsync(GlobalCleanupCalled);

            [Benchmark]
            public Task Benchmark() => Console.Out.WriteLineAsync(BenchmarkCalled);
        }

        [Fact]
        public void AllSetupAndCleanupMethodRunsAsyncTaskSetupTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarksAsyncTaskSetup>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarksAsyncTaskSetup
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public Task GlobalSetup() => Console.Out.WriteLineAsync(GlobalSetupCalled);

            [GlobalCleanup]
            public Task GlobalCleanup() => Console.Out.WriteLineAsync(GlobalCleanupCalled);

            [Benchmark]
            public void Benchmark() => Console.WriteLine(BenchmarkCalled);
        }

        [Fact]
        public void AllSetupAndCleanupMethodRunsAsyncGenericTaskSetupTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarksAsyncGenericTaskSetup>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarksAsyncGenericTaskSetup
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public async Task<int> GlobalSetup()
            {
                await Console.Out.WriteLineAsync(GlobalSetupCalled);

                return 42;
            }

            [GlobalCleanup]
            public async Task<int> GlobalCleanup()
            {
                await Console.Out.WriteLineAsync(GlobalCleanupCalled);

                return 42;
            }

            [Benchmark]
            public void Benchmark() => Console.WriteLine(BenchmarkCalled);
        }

        [Fact]
        public void AllSetupAndCleanupMethodRunsAsyncValueTaskSetupTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarksAsyncValueTaskSetup>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarksAsyncValueTaskSetup
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public ValueTask GlobalSetup() => new ValueTask(Console.Out.WriteLineAsync(GlobalSetupCalled));

            [GlobalCleanup]
            public ValueTask GlobalCleanup() => new ValueTask(Console.Out.WriteLineAsync(GlobalCleanupCalled));

            [Benchmark]
            public void Benchmark() => Console.WriteLine(BenchmarkCalled);
        }

        [FactEnvSpecific(EnvRequirement.NonWindows)]
        public void AllSetupAndCleanupMethodRunsAsyncGenericValueTaskSetupTest()
        {
            var miniJob = Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(2).WithIterationCount(3).WithInvocationCount(1).WithUnrollFactor(1).WithId("MiniJob");
            var config = CreateSimpleConfig(job: miniJob);

            var summary = CanExecute<AllSetupAndCleanupAttributeBenchmarksAsyncGenericValueTaskSetup>(config);

            var actualLogLines = GetActualLogLines(summary);
            foreach (string line in actualLogLines)
                Output.WriteLine(line);
            Assert.Equal(expectedLogLines, actualLogLines);
        }

        public class AllSetupAndCleanupAttributeBenchmarksAsyncGenericValueTaskSetup
        {
            private int setupCounter;
            private int cleanupCounter;

            [IterationSetup]
            public void IterationSetup() => Console.WriteLine(IterationSetupCalled + " (" + ++setupCounter + ")");

            [IterationCleanup]
            public void IterationCleanup() => Console.WriteLine(IterationCleanupCalled + " (" + ++cleanupCounter + ")");

            [GlobalSetup]
            public async ValueTask<int> GlobalSetup()
            {
                await Console.Out.WriteLineAsync(GlobalSetupCalled);

                return 42;
            }

            [GlobalCleanup]
            public async ValueTask<int> GlobalCleanup()
            {
                await Console.Out.WriteLineAsync(GlobalCleanupCalled);

                return 42;
            }

            [Benchmark]
            public void Benchmark() => Console.WriteLine(BenchmarkCalled);
        }
    }
}