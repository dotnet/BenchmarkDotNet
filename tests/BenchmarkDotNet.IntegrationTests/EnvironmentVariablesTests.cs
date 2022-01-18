using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class EnvironmentVariablesTests : BenchmarkTestExecutor
    {
        internal const string Key = "VeryNiceKey";
        internal const string Value = "VeryNiceValue";

        public EnvironmentVariablesTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void UserCanSpecifyEnvironmentVariables()
        {
            var variables = new[] { new EnvironmentVariable(Key, Value) };
            var jobWithCustomConfiguration = Job.Dry.WithEnvironmentVariables(variables);
            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);

            CanExecute<WithEnvironmentVariables>(config);
        }

        public class WithEnvironmentVariables
        {
            [Benchmark]
            public void Benchmark()
            {
                if (Environment.GetEnvironmentVariable(Key) != Value)
                    throw new InvalidOperationException("The env var was not set");
            }
        }

        [Fact]
        public void ResharperDynamicProgramAnalysisIsDisabledByDefault()
            => CanExecute<TestingDpaDisabled>(CreateSimpleConfig(job: Job.Dry));

        public class TestingDpaDisabled
        {
            [Benchmark]
            public void Benchmark()
            {
                if (Environment.GetEnvironmentVariable("JETBRAINS_DPA_AGENT_ENABLE") != "0")
                    throw new InvalidOperationException("The JETBRAINS_DPA_AGENT_ENABLE env var was not set to zero");
            }
        }

        [Fact]
        public void ResharperDynamicProgramAnalysisCanBeEnabled()
        {
            var jobWithSettingEnabled = Job.Dry.WithEnvironmentVariable("JETBRAINS_DPA_AGENT_ENABLE", "1");
            var config = CreateSimpleConfig(job: jobWithSettingEnabled);

            CanExecute<TestingDpaEnabled>(config);
        }

        public class TestingDpaEnabled
        {
            [Benchmark]
            public void Benchmark()
            {
                if (Environment.GetEnvironmentVariable("JETBRAINS_DPA_AGENT_ENABLE") != "1")
                    throw new InvalidOperationException("The JETBRAINS_DPA_AGENT_ENABLE env var was not set to one");
            }
        }

    }
}