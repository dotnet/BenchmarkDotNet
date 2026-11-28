using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General;

public class BenchmarkClassSuppressorTests
{
    public class General : SuppressorTestFixture<BenchmarkClassSuppressor>
    {
        [Fact]
        public async Task Public_class_with_benchmark_method_is_suppressed()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class {|#0:BenchmarkClass|}
                {
                    [Benchmark]
                    public void BenchmarkMethod()
                    {
                    }
                }
                """;

            AddExpectedDiagnostic(0, isSuppressed: true);

            await RunAsync();
        }

        [Fact]
        public async Task Public_class_without_benchmark_method_is_not_suppressed()
        {
            TestCode = /* lang=c#-test */ """
                public class {|#0:Helper|}
                {
                    public void Method()
                    {
                    }
                }
                """;

            AddExpectedDiagnostic(0, isSuppressed: false);

            await RunAsync();
        }

        [Fact]
        public async Task Public_class_with_benchmark_method_in_another_partial_declaration_is_suppressed()
        {
            TestCode = /* lang=c#-test */ """
                public partial class {|#0:BenchmarkClass|}
                {
                }
                """;
            AddSource(/* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public partial class BenchmarkClass
                {
                    [Benchmark]
                    public void BenchmarkMethod()
                    {
                    }
                }
                """);

            AddExpectedDiagnostic(0, isSuppressed: true);

            await RunAsync();
        }

        [Fact]
        public async Task Public_class_with_benchmark_method_using_derived_attribute_is_suppressed()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                internal class CustomBenchmarkAttribute : BenchmarkAttribute
                {
                }

                public class {|#0:BenchmarkClass|}
                {
                    [CustomBenchmark]
                    public void BenchmarkMethod()
                    {
                    }
                }
                """;

            AddExpectedDiagnostic(0, isSuppressed: true);

            await RunAsync();
        }

        [Fact]
        public async Task Public_class_inheriting_benchmark_method_is_suppressed()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class {|#0:BenchmarkBase|}
                {
                    [Benchmark]
                    public void BenchmarkMethod()
                    {
                    }
                }

                public class {|#1:DerivedBenchmarkClass|} : BenchmarkBase
                {
                }
                """;

            AddExpectedDiagnostic(0, isSuppressed: true);
            AddExpectedDiagnostic(1, isSuppressed: true);

            await RunAsync();
        }

        [Fact]
        public async Task Nested_benchmark_class_is_suppressed_but_helpers_are_not()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class {|#0:Outer|}
                {
                    public class {|#1:BenchmarkClass|}
                    {
                        [Benchmark]
                        public void BenchmarkMethod()
                        {
                        }
                    }

                    public class {|#2:Helper|}
                    {
                    }
                }
                """;

            AddExpectedDiagnostic(0, isSuppressed: false);
            AddExpectedDiagnostic(1, isSuppressed: true);
            AddExpectedDiagnostic(2, isSuppressed: false);

            await RunAsync();
        }

        [Fact]
        public async Task Public_class_is_not_suppressed_when_benchmarkdotnet_is_not_referenced()
        {
            TestCode = /* lang=c#-test */ """
                public class {|#0:Helper|}
                {
                }
                """;
            ClearAdditionalReferences();

            AddExpectedDiagnostic(0, isSuppressed: false);

            await RunAsync();
        }
    }
}
