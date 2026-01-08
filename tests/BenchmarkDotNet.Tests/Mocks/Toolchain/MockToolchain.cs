using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Tests.Mocks.Toolchain
{
    public class MockToolchain(Func<BenchmarkCase, List<Measurement>> measurer) : IToolchain
    {
        public string Name => nameof(MockToolchain);
        public IGenerator Generator => new MockGenerator();
        public IBuilder Builder => new MockBuilder();
        public IExecutor Executor { get; private set; } = new MockExecutor(measurer);
        public bool IsInProcess => false;
        public IAsyncEnumerable<ValidationError> ValidateAsync(BenchmarkCase benchmarkCase, IResolver resolver) => AsyncEnumerable.Empty<ValidationError>();

        public override string ToString() => GetType().Name;

        private class MockGenerator : IGenerator
        {
            public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
                => GenerateResult.Success(ArtifactsPaths.Empty, []);
        }

        private class MockBuilder : IBuilder
        {
            public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger) => BuildResult.Success(generateResult);
        }

        private class MockExecutor(Func<BenchmarkCase, List<Measurement>> measurer) : IExecutor
        {
            public ValueTask<ExecuteResult> ExecuteAsync(ExecuteParameters executeParameters) => new(new ExecuteResult(measurer(executeParameters.BenchmarkCase)));
        }
    }
}