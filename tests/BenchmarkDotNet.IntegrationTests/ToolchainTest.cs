using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ToolchainTest(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private class MyGenerator : IGenerator
        {
            public bool Done { get; private set; }

            public ValueTask<GenerateResult> GenerateProjectAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
            {
                logger.WriteLine("Generating");
                Done = true;
                return new(new GenerateResult(ArtifactsPaths.Empty, true, null, []));
            }
        }

        private class MyBuilder : IBuilder
        {
            public bool Done { get; private set; }

            public ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
            {
                logger.WriteLine("Building");
                Done = true;
                return new(BuildResult.Success(generateResult));
            }
        }

        private class MyExecutor : IExecutor
        {
            public bool Done { get; private set; }

            public ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
            {
                executeParameters.Logger.WriteLine("Executing");
                Done = true;
                return new(new ExecuteResult(true, 0, default, [], [], [], executeParameters.LaunchIndex));
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
            var job = new Job(Job.Dry) { Infrastructure = { Toolchain = myToolchain } };
            var config = CreateSimpleConfig(logger).AddJob(job);

            CanExecute<ToolchainBenchmark>(config, fullValidation: false);

            Assert.True(generator.Done);
            Assert.True(builder.Done);
            Assert.True(executor.Done);
        }
    }
}