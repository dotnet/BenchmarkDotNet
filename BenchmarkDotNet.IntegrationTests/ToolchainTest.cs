using System;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Diagnosers;
using BenchmarkDotNet.Plugins.Toolchains;
using BenchmarkDotNet.Plugins.Toolchains.Results;
using BenchmarkDotNet.Tasks;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ToolchainTest
    {
        private class MyGenerator : IBenchmarkGenerator
        {
            public bool Done { get; private set; }

            public BenchmarkGenerateResult GenerateProject(Benchmark benchmark)
            {
                Done = true;
                return new BenchmarkGenerateResult(null, true, null);
            }
        }

        private class MyBuilder : IBenchmarkBuilder
        {
            public bool Done { get; private set; }

            public BenchmarkBuildResult Build(BenchmarkGenerateResult generateResult)
            {
                Done = true;
                return new BenchmarkBuildResult(generateResult, true, null);
            }
        }

        private class MyExecutor : IBenchmarkExecutor
        {
            public bool Done { get; private set; }

            public BenchmarkExecResult Exec(BenchmarkBuildResult buildResult, BenchmarkParameters parameters, IBenchmarkDiagnoser diagnoser)
            {
                Done = true;
                return new BenchmarkExecResult(true, new string[0]);
            }
        }

        [Benchmark]
        [BenchmarkTask(toolchain: BenchmarkToolchain.Custom1, mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
        }

        [Fact]
        public void CustomToolchain()
        {
            var generator = new MyGenerator();
            var builder = new MyBuilder();
            var executor = new MyExecutor();
            var plugins = BenchmarkPluginBuilder.CreateEmpty().
                AddToolchain(new BenchmarkToolchainBuilder(
                    BenchmarkToolchain.Custom1,
                    (benchmark, logger) => generator,
                    (benchmark, logger) => builder,
                    (benchmark, logger) => executor));
            new BenchmarkRunner(plugins).Run<ToolchainTest>();
            Assert.True(generator.Done);
            Assert.True(builder.Done);
            Assert.True(executor.Done);

            Assert.Throws<NotSupportedException>(() => new BenchmarkRunner(BenchmarkPluginBuilder.CreateEmpty()).Run<ToolchainTest>());
        }
    }
}