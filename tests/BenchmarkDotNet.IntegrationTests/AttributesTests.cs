using System;
using BenchmarkDotNet.Attributes;
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
            private const int ExpectedNumber = 123;
            private const string ExpectedText = "expectedTest";

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
                if (ExpectedNumber != Number || ExpectedText != Text)
                    throw new Exception("Custom attributes were not applied!");
            }
        }

        private class CustomParamsAttribute : ParamsAttribute
        {
            public CustomParamsAttribute(params object[] values) : base(values) { }
        }

        private class CustomBenchmarkAttribute : BenchmarkAttribute { }

        private class CustomGlobalSetupAttribute : GlobalSetupAttribute { }
    }
}