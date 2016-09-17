using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
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

            public GenerateResult GenerateProject(Benchmark benchmark, ILogger logger, string rootArtifactsFolderPath, IConfig config, IResolver resolver)
            {
                logger.WriteLine("Generating");
                Done = true;
                return new GenerateResult(null, true, null);
            }
        }

        private class MyBuilder : IBuilder
        {
            public bool Done { get; private set; }

            public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark, IResolver resolver)
            {
                logger.WriteLine("Building");
                Done = true;
                return BuildResult.Success(generateResult);
            }
        }

        private class MyExecutor : IExecutor
        {
            public bool Done { get; private set; }

            public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser diagnoser)
            {
                logger.WriteLine("Executing");
                Done = true;
                return new ExecuteResult(true, 0, new string[0]);
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
            var job = Job.Dry.With(myToolchain);
            var config = CreateSimpleConfig(logger).With(job);

            CanExecute<ToolchainBenchmark>(config, fullValidation: false);

            Assert.True(generator.Done);
            Assert.True(builder.Done);
            Assert.True(executor.Done);
        }
    }
}