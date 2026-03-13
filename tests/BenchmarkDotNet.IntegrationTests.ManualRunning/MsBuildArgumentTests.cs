using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning
{
    public class MsBuildArgumentTests : BenchmarkTestExecutor
    {
        private const string CustomPropEnvVarName = "CustomPropEnvVarName";

        public MsBuildArgumentTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ProcessIsBuiltWithCustomProperty(bool setCustomProperty)
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildArgument($"/p:CustomProp={setCustomProperty}")])
                    .WithEnvironmentVariable(CustomPropEnvVarName, setCustomProperty.ToString())
                );
            CanExecute<PropertyDefine>(config);
        }

        [Fact]
        public void MultipleProcessesAreBuiltWithCorrectProperties()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildArgument($"/p:CustomProp={true}")])
                    .WithEnvironmentVariable(CustomPropEnvVarName, true.ToString())
                )
                .AddJob(Job.Dry
                    .WithRuntime(NativeAotRuntime.Net80)
                    .WithArguments([new MsBuildArgument($"/p:CustomProp={true}")])
                    .WithEnvironmentVariable(CustomPropEnvVarName, true.ToString())
                )
                .AddJob(Job.Dry
                    .WithEnvironmentVariable(CustomPropEnvVarName, false.ToString())
                );
            CanExecute<PropertyDefine>(config);
        }

        [Fact]
        public void EscapesSemicolonInDefineConstants()
        {
            var arg = new MsBuildArgument("/p:DefineConstants=TEST1;TEST2");
            Assert.Equal("/p:DefineConstants=TEST1%3BTEST2", arg.ToString());
        }

        [Fact]
        public void EscapesPercentSign()
        {
            var arg = new MsBuildArgument("/p:SomeValue=100%");
            Assert.Equal("/p:SomeValue=100%25", arg.ToString());
        }

        [Fact]
        public void DoesNotDoubleEscapeAlreadyEscapedPercent()
        {
            var arg = new MsBuildArgument("/p:SomeValue=100%25", false);
            Assert.Equal("/p:SomeValue=100%25", arg.ToString());
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