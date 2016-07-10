using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class AttributesTests
    {
        private readonly ITestOutputHelper output;

        public AttributesTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void AreNotSealed()
        {
            var config = new SingleRunFastConfig()
                .With(new OutputLogger(output))
                .With(DefaultConfig.Instance.GetColumns().ToArray());

            BenchmarkTestExecutor.CanExecute<ConsumingCustomAttributes>(config);
        }

        public class ConsumingCustomAttributes
        {
            const int ExpectedNumber = 123;
            const string ExpectedText = "expectedTest";

            [CustomParams(ExpectedNumber)]
            public int Number;

            public string Text;

            [CustomSetup]
            public void Setup()
            {
                Text = ExpectedText;
            }

            [CustomBenchmark]
            public void Benchmark()
            {
                Assert.Equal(ExpectedNumber, Number);
                Assert.Equal(ExpectedText, Text);
            }
        }

        class CustomParamsAttribute : ParamsAttribute
        {
            public CustomParamsAttribute(params object[] values) : base(values) { }
        }

        class CustomBenchmarkAttribute : BenchmarkAttribute { }

        class CustomSetupAttribute : SetupAttribute { }
    }
}