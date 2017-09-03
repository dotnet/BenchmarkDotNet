using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.IntegrationTests.Xunit;
using BenchmarkDotNet.Jobs;
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

        [FactClassicDotNetOnly("ProcessStartInfo.EnvironmentVariables is avaialable for .NET Core 2.0+")]
        public void UserCanSpecifyEnvironmentVariables()
        {
            var variables = new [] { new EnvironmentVariable(Key, Value) };
            var jobWithCustomConfiguration = JobExtensions.With(Job.Dry, variables);
            var config = CreateSimpleConfig(job: jobWithCustomConfiguration);

            CanExecute<WithEnvironmentVariables>(config);
        }

        public class WithEnvironmentVariables
        {
            [Benchmark]
            public void Benchmark()
            {
                if(Environment.GetEnvironmentVariable(Key) != Value)
                    throw new InvalidOperationException("The env var was not set");
            }
        }
    }
}