using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AttributesTests : BenchmarkTestExecutor
    {
        public AttributesTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void AttributesAreNotSealed()
        {
            CanExecute<ConsumingCustomAttributes>();
        }

        public class ConsumingCustomAttributes
        {
            const int ExpectedNumber = 123;
            const string ExpectedText = "expectedTest";

            [CustomParams(ExpectedNumber)]
            public int Number;

            public string Text;

            [CustomGlobalSetup]
            public void Setup()
            {
                Text = ExpectedText;
            }

            [CustomBenchmark]
            public void Benchmark()
            {
                if(ExpectedNumber != Number || ExpectedText != Text)
                    throw new Exception("Custom attributes were not applied!");
            }
        }

        class CustomParamsAttribute : ParamsAttribute
        {
            public CustomParamsAttribute(params object[] values) : base(values) { }
        }

        class CustomBenchmarkAttribute : BenchmarkAttribute { }

        class CustomGlobalSetupAttribute : GlobalSetupAttribute { }
    }
}