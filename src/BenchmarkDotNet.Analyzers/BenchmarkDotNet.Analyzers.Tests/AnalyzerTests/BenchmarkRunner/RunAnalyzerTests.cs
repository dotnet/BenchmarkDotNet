namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.BenchmarkRunner
{
    using Fixtures;

    using Analyzers.BenchmarkRunner;

    using Xunit;

    using System.Threading.Tasks;

    public class RunAnalyzerTests
    {
        public class TypeArgumentClassMissingBenchmarkMethods : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMissingBenchmarkMethods() : base(RunAnalyzer.TypeArgumentClassMissingBenchmarkMethodsRule) { }

            [Fact]
            public async Task Invoking_with_type_argument_class_having_only_one_and_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Running;

public class Program
{{
    public static void Main(string[] args) {{
        BenchmarkRunner.Run<{classWithOneBenchmarkMethodName}>();
    }}
}}";

                var benchmarkClassDocument =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class {classWithOneBenchmarkMethodName}
{{
    [Benchmark]
    public void BenchmarkMethod()
    {{

    }}
}}";

                TestCode = testCode;
                AddSource(benchmarkClassDocument);

                await RunAsync();
            }

            [Fact]
            public async Task Invoking_with_type_argument_class_having_no_public_method_annotated_with_the_benchmark_attribute_should_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Running;

public class Program
{{
    public static void Main(string[] args) {{
        BenchmarkRunner.Run<{{|#0:{classWithOneBenchmarkMethodName}|}}>();
    }}
}}";

                var benchmarkClassDocument =
/* lang=c#-test */ $@"public class {classWithOneBenchmarkMethodName}
{{
    public void BenchmarkMethod()
    {{

    }}
}}";
                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddDefaultExpectedDiagnostic(classWithOneBenchmarkMethodName);

                await RunAsync();
            }

            [Fact]
            public async Task Invoking_with_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Running;

public class Program
{{
    public static void Main(string[] args) {{
        BenchmarkRunner.Run<{classWithOneBenchmarkMethodName}>();
    }}
}}";

                var benchmarkClassDocument =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class {classWithOneBenchmarkMethodName}
{{
    [Benchmark]
    public void BenchmarkMethod()
    {{

    }}
    
    public void BenchmarkMethod2()
    {{

    }}
    
    private void BenchmarkMethod3()
    {{
                                                                           
    }}
}}";

                TestCode = testCode;
                AddSource(benchmarkClassDocument);

                await RunAsync();
            }
        }
    }
}
