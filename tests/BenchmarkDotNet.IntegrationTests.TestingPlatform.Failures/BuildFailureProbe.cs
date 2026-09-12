using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.IntegrationTests.TestingPlatform.Failures
{
    /// <summary>
    /// A benchmark whose build always fails, so that the adapter has to turn the build failure into a failed test.
    /// The failure is faked by the toolchain rather than by uncompilable code, which keeps it quick and keeps the
    /// error message the test asserts on under this file's control.
    /// </summary>
    [Config(typeof(FailingBuildConfig))]
    public class BuildFailureProbe
    {
        internal const string ErrorMessage = "The build of this benchmark always fails, on purpose.";

        [Benchmark]
        public int Add() => 1 + 1;

        private class FailingBuildConfig : ManualConfig
        {
            public FailingBuildConfig()
                => AddJob(Job.Dry.WithToolchain(new FailingBuildToolchain()));
        }

        private sealed class FailingBuildToolchain()
            : Toolchain("FailingBuild", UnknownRuntime.Instance, new NoopGenerator(), new FailingBuilder(), new UnreachableExecutor())
        {
        }

        private sealed class NoopGenerator : IGenerator
        {
            public ValueTask<GenerateResult> GenerateProjectAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
                => new(GenerateResult.Success(ArtifactsPaths.Empty, []));
        }

        private sealed class FailingBuilder : IBuilder
        {
            public ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
                => new(BuildResult.Failure(generateResult, ErrorMessage));
        }

        private sealed class UnreachableExecutor : IExecutor
        {
            // A benchmark that failed to build is never executed.
            public ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters, CancellationToken cancellationToken)
                => throw new InvalidOperationException("The benchmark should never have been executed.");
        }
    }
}
