using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ToolchainTest : BenchmarkTestExecutor
    {
        public ToolchainTest(ITestOutputHelper output) : base(output) { }

        private class MyGenerator : IGenerator
        {
            public bool Done { get; private set; }

            public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            {
                logger.WriteLine("Generating");
                Done = true;
                return new GenerateResult(null, true, null, Array.Empty<string>());
            }
        }

        private class MyBuilder : IBuilder
        {
            public bool Done { get; private set; }

            public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
            {
                logger.WriteLine("Building");
                Done = true;
                return BuildResult.Success(generateResult);
            }
        }

        private class MyExecutor : IExecutor
        {
            public bool Done { get; private set; }

            public ExecuteResult Execute(ExecuteParameters executeParameters)
            {
                executeParameters.Logger.WriteLine("Executing");
                Done = true;
                return new ExecuteResult(true, 0, default, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), executeParameters.LaunchIndex);
            }
        }

        public class ToolchainBenchmark
        {
            [Benchmark]
            public void Benchmark()
            {
            }
        }

        [Fact]
        public void CustomToolchainsAreSupported()
        {
            var logger = new OutputLogger(Output);

            var generator = new MyGenerator();
            var builder = new MyBuilder();
            var executor = new MyExecutor();
            var myToolchain = new Toolchain("My", generator, builder, executor);
            var job = new Job(Job.Dry) { Infrastructure = { Toolchain = myToolchain} };
            var config = CreateSimpleConfig(logger).AddJob(job);

            CanExecute<ToolchainBenchmark>(config, fullValidation: false);

            Assert.True(generator.Done);
            Assert.True(builder.Done);
            Assert.True(executor.Done);
        }
    }
}