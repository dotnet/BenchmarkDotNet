using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Parameters;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Tests.Mocks.Toolchain
{
    public class MockToolchain : IToolchain
    {
        private readonly IMockMeasurer measurer;

        public MockToolchain(IMockMeasurer measurer) => this.measurer = measurer;

        public string Name => nameof(MockToolchain);
        public IGenerator Generator => new MockGenerator();
        public IBuilder Builder => new MockBuilder();
        public IExecutor Executor => new MockExecutor(measurer);
        public bool IsInProcess => false;
        public IEnumerable<ValidationError> Validate(BenchmarkCase benchmarkCase, IResolver resolver) => ImmutableArray<ValidationError>.Empty;

        public override string ToString() => GetType().Name;

        private class MockGenerator : IGenerator
        {
            public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
                => GenerateResult.Success(ArtifactsPaths.Empty, ImmutableArray<string>.Empty);
        }

        private class MockBuilder : IBuilder
        {
            public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger) => BuildResult.Success(generateResult);
        }

        private class MockExecutor : IExecutor
        {
            private readonly IMockMeasurer mockMeasurer;

            public MockExecutor(IMockMeasurer mockMeasurer) => this.mockMeasurer = mockMeasurer;

            public ExecuteResult Execute(ExecuteParameters executeParameters) => new (mockMeasurer.Measure(executeParameters.BenchmarkCase));
        }
    }
}