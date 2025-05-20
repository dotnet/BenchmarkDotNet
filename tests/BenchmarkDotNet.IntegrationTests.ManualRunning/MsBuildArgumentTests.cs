using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using System;
using System.Linq;
using System.Reflection;
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

        private const string AsciiChars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        [Theory]
        [InlineData("AAA;BBB")]  // Contains separator char (`;`)
        [InlineData(AsciiChars)] // Validate all ASCII char patterns.
        // Following tests are commented out by default. Because it takes time to execute.
        //[InlineData("AAA;BBB,CCC")] // Validate argument that contains semicolon/comma separators.
        //[InlineData("AAA BBB")]     // Contains space char
        //[InlineData("AAA\"BBB")]    // Contains double quote char
        //[InlineData("AAA\\BBB")]    // Contains backslash char
        //[InlineData("\"AAA")]       // StartsWith `"` but not ends with `"`
        //[InlineData("AAA\"")]       // EndsWith `"` but not ends with `"`
        //[InlineData("\"AAA;BBB\"", "AAA;BBB")]       // Surrounded with `"`
        //[InlineData("\\\"AAA%3BBBB\\\"", "AAA;BBB")] // Surrounded with `\"` and escaped `;`
        public void ValidateEscapedMsBuildArgument(string propertyValue, string? expectedValue = null)
        {
            // Arrange
            expectedValue ??= propertyValue;
            var config = ManualConfig.CreateEmpty()
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .AddJob(Job.Dry
                    .WithStrategy(RunStrategy.Monitoring)
                    .WithArguments([new MsBuildArgument($"/p:CustomProp={propertyValue}", escape: true)])
                    .WithEnvironmentVariable(CustomPropEnvVarName, expectedValue)
                );

            // Act
            var summary = CanExecute<ValidateEscapedValueBenchmark>(config, fullValidation: false);

            // Assert
            Assert.False(summary.HasCriticalValidationErrors);
            Assert.True(summary.Reports.Any());
            foreach (var report in summary.Reports)
            {
                if (!report.GenerateResult.IsGenerateSuccess)
                {
                    var message = report.GenerateResult.GenerateException?.ToString();
                    Assert.Fail($"Failed to generate benchmark project:{Environment.NewLine}{message}");
                }

                if (!report.BuildResult.IsBuildSuccess)
                    Assert.Fail($"Failed to build benchmark project:{Environment.NewLine}{report.BuildResult.ErrorMessage}");

                foreach (var executeResult in report.ExecuteResults)
                {
                    if (!executeResult.IsSuccess)
                    {
                        string message = string.Join(Environment.NewLine, executeResult.Results).Trim();
                        Assert.Fail($"Failed to run benchmark({report.BenchmarkCase.Descriptor.DisplayInfo}):{Environment.NewLine}{message}");
                    }
                }
            }
        }

        public class ValidateEscapedValueBenchmark
        {
            [Benchmark]
            public void Validate()
            {
                // Gets expected value from environment variable.
                var expected = Environment.GetEnvironmentVariable(CustomPropEnvVarName);

                // Gets MSBuild property from AssemblyMetadataAttribute (This attribute is set by csproj)
                var result = typeof(MsBuildArgumentTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(x => x.Key == "CustomProp").Value;

                if (result != expected)
                    throw new Exception($"Failed to run benchmark. Expected:{expected} Actual : {result}");
            }
        }
    }
}
