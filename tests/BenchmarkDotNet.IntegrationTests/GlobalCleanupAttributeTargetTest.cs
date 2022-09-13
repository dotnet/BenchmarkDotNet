﻿using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GlobalCleanupAttributeTargetTest : BenchmarkTestExecutor
    {
        private const string BaselineGlobalCleanupCalled = "// ### Baseline GlobalCleanup called ###";
        private const string BaselineBenchmarkCalled = "// ### Baseline Benchmark called ###";
        private const string FirstGlobalCleanupCalled = "// ### First GlobalCleanup called ###";
        private const string FirstBenchmarkCalled = "// ### First Benchmark called ###";
        private const string SecondGlobalCleanupCalled = "// ### Second GlobalCleanup called ###";
        private const string SecondBenchmarkCalled = "// ### Second Benchmark called ###";

        public GlobalCleanupAttributeTargetTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalCleanupTargetSpecificMethodTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            var summary = CanExecute<GlobalCleanupAttributeTargetBenchmarks>(config);

            IReadOnlyList<string> standardOuput = GetCombinedStandardOutput(summary);

            string[] expected = new string[]
            {
                BaselineBenchmarkCalled,
                BaselineGlobalCleanupCalled,
                FirstBenchmarkCalled,
                FirstGlobalCleanupCalled,
                SecondBenchmarkCalled,
                SecondGlobalCleanupCalled
            };

            Assert.Equal(expected, standardOuput);
        }

        public class GlobalCleanupAttributeTargetBenchmarks
        {
            private int cleanupValue;

            [Benchmark(Baseline = true)]
            public void BaselineBenchmark()
            {
                cleanupValue = -1;

                Console.WriteLine(BaselineBenchmarkCalled);
            }

            [GlobalCleanup]
            public void BaselineCleanup()
            {
                Assert.Equal(-1, cleanupValue);

                Console.WriteLine(BaselineGlobalCleanupCalled);
            }

            [Benchmark]
            public void Benchmark1()
            {
                cleanupValue = 1;

                Console.WriteLine(FirstBenchmarkCalled);
            }

            [GlobalCleanup(Target = nameof(Benchmark1))]
            public void GlobalCleanup1()
            {
                Assert.Equal(1, cleanupValue);

                Console.WriteLine(FirstGlobalCleanupCalled);
            }

            [Benchmark]
            public void Benchmark2()
            {
                cleanupValue = 2;

                Console.WriteLine(SecondBenchmarkCalled);
            }

            [GlobalCleanup(Target = nameof(Benchmark2))]
            public void GlobalCleanup2()
            {
                Assert.Equal(2, cleanupValue);

                Console.WriteLine(SecondGlobalCleanupCalled);
            }
        }
    }
}
