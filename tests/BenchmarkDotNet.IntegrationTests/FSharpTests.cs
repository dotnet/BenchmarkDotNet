using System;
using Xunit;
using Xunit.Abstractions;

using BenchmarkDotNet.Tests.Loggers;
using static FSharpBenchmarks;

namespace BenchmarkDotNet.IntegrationTests
{
    public class FSharpTests : BenchmarkTestExecutor
    {
        public FSharpTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ParamsSupportFSharpEnums()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<EnumParamsTest>(config);
            foreach (var param in new[] { TestEnum.A, TestEnum.B })
                Assert.Contains($"// ### New Parameter {param} ###" + Environment.NewLine, logger.GetLog());
            Assert.DoesNotContain($"// ### New Parameter {TestEnum.C} ###" + Environment.NewLine, logger.GetLog());
        }
    }
}
