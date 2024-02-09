using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class MsBuildArgumentTests : BenchmarkTestExecutor
    {
        private const string CustomPropEnvVarName = "CustomPropEnvVarName";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ProcessIsBuiltWithCustomProperty(bool setCustomProperty)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithArguments(new Argument[] { new MsBuildArgument($"/p:CustomProp={setCustomProperty}") })
                    .WithEnvironmentVariable(CustomPropEnvVarName, setCustomProperty.ToString())
                );
            CanExecute<PropertyDefine>(config);
        }

        [Fact]
        public void MultipleProcessesAreBuiltWithCorrectProperties()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithArguments(new Argument[] { new MsBuildArgument($"/p:CustomProp={true}") })
                    .WithEnvironmentVariable(CustomPropEnvVarName, true.ToString())
                )
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net80)
                    .WithArguments(new Argument[] { new MsBuildArgument($"/p:CustomProp={true}") })
                    .WithEnvironmentVariable(CustomPropEnvVarName, true.ToString())
                )
                .AddJob(Job.Dry
                    .WithEnvironmentVariable(CustomPropEnvVarName, false.ToString())
                );
            CanExecute<PropertyDefine>(config);
        }

        public class PropertyDefine
        {
            private const bool customPropWasSet =
#if CUSTOM_PROP
                true;
#else
                false;
#endif

            [Benchmark]
            public void ThrowWhenWrong()
            {
                if (Environment.GetEnvironmentVariable(CustomPropEnvVarName) != customPropWasSet.ToString())
                {
                    throw new InvalidOperationException($"Custom property was not set properly, the expected value was {Environment.GetEnvironmentVariable(CustomPropEnvVarName)}");
                }
            }
        }
    }
}