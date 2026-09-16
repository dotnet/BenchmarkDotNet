using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.IntegrationTests
{
    public sealed class MockToolchain(string name, Runtime runtime, IBuilder builder, IExecutor executor)
        : Toolchain(name, runtime, builder, executor)
    {
    }

    public class ToolchainTest(ITestOutputHelper output) : BenchmarkTestExecutor(output)
    {
        private class MyBuilder : IBuilder
        {
            public bool Done { get; private set; }

            public bool GetSupportsConcurrency(BuildPartition buildPartition) => true;

            public ValueTask<BuildResult> BuildAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
            {
                logger.WriteLine("Building");
                Done = true;
                return new(BuildResult.Success(ArtifactsPaths.Empty));
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

            var builder = new MyBuilder();
            var executor = new MyExecutor();
            var myToolchain = new MockToolchain("My", UnknownRuntime.Instance, builder, executor);
            var job = new Job(Job.Dry) { Infrastructure = { Toolchain = myToolchain } };
            var config = CreateSimpleConfig(logger).AddJob(job);

            CanExecute<ToolchainBenchmark>(config, fullValidation: false);

            Assert.True(builder.Done);
            Assert.True(executor.Done);
        }
    }
}