namespace TestDriven.Benchmark.Tests
{
    using System;
    using System.Reflection;
    using BenchmarkDotNet.Running;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.TestDriven;
    using BenchmarkDotNet.Jobs;
    using NSubstitute;
    using Framework;
    using NUnit.Framework;

    public class BenckmarkTestRunnerTests
    {
        [Test]
        public void RunMember_NoBenchmarks_NoTests()
        {
            var benchmarkTestRunner = Substitute.For<BenchmarkTestRunner>();
            benchmarkTestRunner.GetConfig().Returns(new SuperQuickConfig());
            var testListener = Substitute.For<ITestListener>();
            var targetType = typeof(NoBenchmarks).GetTypeInfo();

            var testRunState = benchmarkTestRunner.RunMember(testListener, targetType.Assembly, targetType);

            Assert.That(testRunState, Is.EqualTo(TestRunState.NoTests));
        }

        [Test]
        public void RunMember_SomeBenchmarks_Success2Tests()
        {
            var benchmarkTestRunner = Substitute.For<BenchmarkTestRunner>();
            benchmarkTestRunner.GetConfig().Returns(new SuperQuickConfig());
            var testListener = Substitute.For<ITestListener>();
            var targetType = typeof(SomeBenchmarks).GetTypeInfo();

            var testRunState = benchmarkTestRunner.RunMember(testListener, targetType.Assembly, targetType);

            Assert.That(testRunState, Is.EqualTo(TestRunState.Success));
            testListener.Received(2).TestFinished(Arg.Any<TestResult>());
        }

        [Test]
        public void RunMember_SomeBenchmarks_TestRunnerName()
        {
            var benchmarkTestRunner = Substitute.For<BenchmarkTestRunner>();
            benchmarkTestRunner.GetConfig().Returns(new SuperQuickConfig());
            var testListener = Substitute.For<ITestListener>();
            var targetType = typeof(SomeBenchmarks).GetTypeInfo();
            var version = typeof(BenchmarkAttribute).GetTypeInfo().Assembly.GetName().Version;
            var expectedTestRunnerName = string.Format("BenchmarkDotNet {0}.{1}.{2}", version.Major, version.Minor, version.Build);

            var testRunState = benchmarkTestRunner.RunMember(testListener, targetType.Assembly, targetType);

            testListener.Received().TestFinished(Arg.Is<TestResult>(r => r.TestRunnerName == expectedTestRunnerName));
        }

        [Test]
        public void RunMember_SomeBenchmarks_TestResultName()
        {
            var benchmarkTestRunner = Substitute.For<BenchmarkTestRunner>();
            benchmarkTestRunner.GetConfig().Returns(new SuperQuickConfig());
            var testListener = Substitute.For<ITestListener>();
            var targetType = typeof(SomeBenchmarks).GetTypeInfo();
            var method = new Action(new SomeBenchmarks().Benchmark1).GetMethodInfo();
            var testName = method.DeclaringType.FullName + "." + method.Name;

            var testRunState = benchmarkTestRunner.RunMember(testListener, targetType.Assembly, targetType);

            testListener.Received().TestFinished(Arg.Is<TestResult>(r => r.Name == testName));
        }

        [Test]
        public void RunMember_SomeBenchmarksBenchmark1_TestResultName()
        {
            var benchmarkTestRunner = Substitute.For<BenchmarkTestRunner>();
            benchmarkTestRunner.GetConfig().Returns(new SuperQuickConfig());
            var testListener = Substitute.For<ITestListener>();
            var targetType = typeof(SomeBenchmarks).GetTypeInfo();
            var method = new Action(new SomeBenchmarks().Benchmark1).GetMethodInfo();
            var testName = method.DeclaringType.FullName + "." + method.Name;

            var testRunState = benchmarkTestRunner.RunMember(testListener, targetType.Assembly, method);

            testListener.Received(1).TestFinished(Arg.Any<TestResult>());
            testListener.Received().TestFinished(Arg.Is<TestResult>(r => r.Name == testName));
        }
    }

    class SuperQuickConfig : ManualConfig
    {
        internal SuperQuickConfig()
        {
            Add(Job.Default
                .WithLaunchCount(1)     // benchmark process will be launched only once
                .WithIterationTime(1)   // 1ms per iteration
                .WithWarmupCount(1)     // 1 warmup iteration
                .WithTargetCount(1)     // 1 target iteration
            );
        }
    }

    public class NoBenchmarks
    {
    }

    public class SomeBenchmarks
    {
        [Benchmark]
        public void Benchmark1()
        {
        }

        [Benchmark]
        public void Benchmark2()
        {
        }
    }
}
