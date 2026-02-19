using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;
public class GeneralArgumentAttributesAnalyzerTests
{
    public class MethodWithoutAttributeMustHaveNoParameters : AnalyzerTestFixture<GeneralArgumentAttributesAnalyzer>
    {
        public MethodWithoutAttributeMustHaveNoParameters() : base(GeneralArgumentAttributesAnalyzer.MethodWithoutAttributeMustHaveNoParametersRule) { }

        [Theory]
        [InlineData("""ArgumentsSource("test")""")]
        [InlineData("""Arguments(42, "test")""")]
        [InlineData("""
                    Arguments(42, "test"), Arguments(1, "test2")
                    """)]
        public async Task A_method_with_parameters_annotated_with_an_argumentssource_or_arguments_attribute_and_the_benchmark_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{attributeUsage}}]
                    public void BenchmarkMethod(int a, string b)
                    {

                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(ParametersListLength))]
        public async Task A_method_with_parameters_and_no_argumentssource_arguments_or_benchmark_attributes_should_not_trigger_diagnostic(int parametersListLength)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public void BenchmarkMethod({{string.Join(", ", Parameters.Take(parametersListLength))}})
                    {

                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(ParametersListLength))]
        public async Task A_method_with_parameters_annotated_with_the_benchmark_attribute_and_an_argumentssource_attribute_but_no_arguments_attribute_should_not_trigger_diagnostic(int parametersListLength)
        {
            const string benchmarkMethodName = "BenchmarkMethod";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [ArgumentsSource("test")]
                    public void {{benchmarkMethodName}}({|#0:{{string.Join(", ", Parameters.Take(parametersListLength))}}|})
                    {

                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(ParametersListLength))]
        public async Task A_method_with_parameters_annotated_with_the_benchmark_attribute_but_no_argumentssource_or_arguments_attribute_should_trigger_diagnostic(int parametersListLength)
        {
            const string benchmarkMethodName = "BenchmarkMethod";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public void {{benchmarkMethodName}}({|#0:{{string.Join(", ", Parameters.Take(parametersListLength))}}|})
                    {

                    }
                }
                """;

            TestCode = testCode;
            AddDefaultExpectedDiagnostic(benchmarkMethodName);

            await RunAsync();
        }

        public static TheoryData<int> ParametersListLength => [.. Enumerable.Range(1, Parameters.Count)];

        private static IReadOnlyCollection<string> Parameters => ["int a", "string b", "bool c"];
    }
}