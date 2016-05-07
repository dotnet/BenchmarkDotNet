using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ToolchainTest
    {
        private readonly ITestOutputHelper output;

        public ToolchainTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        private class MyGenerator : IGenerator
        {
            public bool Done { get; private set; }

            public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath)
            {
                logger.WriteLine("Generating");
                Done = true;
                return new GenerateResult(null, true, null);
            }
        }

        private class MyBuilder : IBuilder
        {
            public bool Done { get; private set; }

            public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
            {
                logger.WriteLine("Building");
                Done = true;
                return new BuildResult(generateResult, true, null, "doesNotMatterHere");
            }
        }

        private class MyExecutor : IExecutor
        {
            public bool Done { get; private set; }

            public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IDiagnoser diagnoser)
            {
                logger.WriteLine("Executing");
                Done = true;
                return new ExecuteResult(true, new string[0]);
            }
        }

        [Benchmark]
        public void Benchmark()
        {
        }

        [Fact]
        public void CustomToolchain()
        {
            var logger = new OutputLogger(output);

            var generator = new MyGenerator();
            var builder = new MyBuilder();
            var executor = new MyExecutor();
            var myToolchain = new Toolchain("My", generator, builder, executor);
            var job = Job.Default.With(myToolchain).With(Mode.SingleRun).WithLaunchCount(1).WithWarmupCount(1).WithTargetCount(1);

            var config = DefaultConfig.Instance.With(job).With(logger);
            BenchmarkRunner.Run<ToolchainTest>(config);
            Assert.True(generator.Done);
            Assert.True(builder.Done);
            Assert.True(executor.Done);
        }
    }
}