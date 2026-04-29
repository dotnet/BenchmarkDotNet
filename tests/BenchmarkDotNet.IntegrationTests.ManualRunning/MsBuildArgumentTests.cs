using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

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
        public void ProcessIsBuiltWithSemicolonSeparatedPropertyValues()
        {
            var config = ManualConfig.CreateEmpty()
                .AddJob(Job.Dry
                    .WithArguments([new MsBuildProperty("ExtraDefineConstants", "TEST1", "TEST2")])
                );

            CanExecute<ExtraDefineConstants>(config);
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

        public class ExtraDefineConstants
        {
            [Benchmark]
            public void ThrowWhenWrong()
            {
#if TEST1 && TEST2
                return;
#else
                throw new InvalidOperationException("ExtraDefineConstants was not applied to the benchmark build.");
#endif
            }
        }
    }
}
