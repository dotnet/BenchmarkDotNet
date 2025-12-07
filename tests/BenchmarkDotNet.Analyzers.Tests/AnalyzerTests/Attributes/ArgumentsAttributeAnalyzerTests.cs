using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;

public class ArgumentsAttributeAnalyzerTests
{
    public class RequiresParameters : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
    {
        public RequiresParameters() : base(ArgumentsAttributeAnalyzer.RequiresParametersRule) { }

        [Theory, CombinatorialData]
        public async Task A_method_annotated_with_an_arguments_attribute_and_the_benchmark_attribute_and_having_no_parameters_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(ArgumentsAttributeUsagesWithLocationMarker))] string emptyArgumentsAttributeUsage)
        {
            const string benchmarkMethodName = "BenchmarkMethod";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    {{emptyArgumentsAttributeUsage}}
                    public void {{benchmarkMethodName}}()
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;

            AddDefaultExpectedDiagnostic(benchmarkMethodName);
            await RunAsync();
        }

        public static IEnumerable<string> ArgumentsAttributeUsagesWithLocationMarker()
        {
            yield return "[{|#0:Arguments|}]";
            yield return "[{|#0:Arguments()|}]";
            yield return "[{|#0:Arguments(Priority = 1)|}]";

            string[] nameColonUsages =
            [
                "",
                "values: "
            ];

            string[] priorityNamedParameterUsages =
            [
                "",
                ", Priority = 1"
            ];

            string[] attributeUsagesBase =
            [
                "Arguments({0}new object[] {{ }}{1})",
                "Arguments({0}new object[0]{1})",
                "Arguments({0}[]{1})",
                "Arguments({0}new object[] {{ 1, 2 }}{1})",
                "Arguments({0}[1, 2]{1})",
            ];

            foreach (var attributeUsageBase in attributeUsagesBase)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                    {
                        yield return $"[{{|#0:{string.Format(attributeUsageBase, nameColonUsage, priorityNamedParameterUsage)}|}}]";
                    }
                }
            }
        }
    }

    public class RequiresBenchmarkAttribute : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
    {
        public RequiresBenchmarkAttribute() : base(ArgumentsAttributeAnalyzer.RequiresBenchmarkAttributeRule) { }

        [Theory]
        [MemberData(nameof(ArgumentAttributeUsagesListLength))]
        public async Task A_method_annotated_with_at_least_one_arguments_attribute_together_with_the_benchmark_attribute_should_not_trigger_diagnostic(int argumentAttributeUsagesListLength)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{string.Join("]\n[", ArgumentAttributeUsages.Take(argumentAttributeUsagesListLength))}}]
                    public void BenchmarkMethod()
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(ArgumentAttributeUsagesListLength))]
        public async Task A_method_with_at_least_one_arguments_attribute_but_no_benchmark_attribute_should_trigger_diagnostic(int argumentAttributeUsagesListLength)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;
                                                
                public class BenchmarkClass
                {
                    {{string.Join("\n", ArgumentAttributeUsages.Take(argumentAttributeUsagesListLength).Select((a, i) => $"[{{|#{i}:{a}|}}]"))}}
                    public void BenchmarkMethod()
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;

            for (var i = 0; i < argumentAttributeUsagesListLength; i++)
            {
                AddExpectedDiagnostic(i);
            }

            await RunAsync();
        }

        public static TheoryData<int> ArgumentAttributeUsagesListLength => [.. Enumerable.Range(1, ArgumentAttributeUsages.Count)];

        private static IReadOnlyCollection<string> ArgumentAttributeUsages =>
        [
            "Arguments",
            "Arguments()",
            "Arguments(42, \"test\")"
        ];
    }

    public class MustHaveMatchingValueCount : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
    {
        public MustHaveMatchingValueCount() : base(ArgumentsAttributeAnalyzer.MustHaveMatchingValueCountRule) { }

        [Fact]
        public async Task A_method_not_annotated_with_any_arguments_attributes_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public void BenchmarkMethod()
                    {
                                                           
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(ArgumentsAttributeUsages))]
        public async Task Having_a_matching_value_count_should_not_trigger_diagnostic(string argumentsAttributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    {{argumentsAttributeUsage}}
                    public void BenchmarkMethod(string a, bool b)
                    {

                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(EmptyArgumentsAttributeUsagesWithLocationMarker))]
        public async Task Having_a_mismatching_empty_value_count_targeting_a_method_with_parameters_should_trigger_diagnostic(string argumentsAttributeUsage)
        {
            const string benchmarkMethodName = "BenchmarkMethod";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    {{argumentsAttributeUsage}}
                    public void {{benchmarkMethodName}}(string a)
                    {
                                                    
                    }
                }
                """;
            TestCode = testCode;
            AddDefaultExpectedDiagnostic(1, "", benchmarkMethodName, 0);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Having_a_mismatching_value_count_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(ArgumentsAttributeUsagesWithLocationMarker))] string argumentsAttributeUsage)
        {
            const string benchmarkMethodName = "BenchmarkMethod";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    {{argumentsAttributeUsage}}
                    public void {{benchmarkMethodName}}(string a)
                    {
                                                    
                    }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, 1, "", benchmarkMethodName, 2);
            AddExpectedDiagnostic(1, 1, "", benchmarkMethodName, 3);

            await RunAsync();
        }

        public static TheoryData<string> ArgumentsAttributeUsages()
        {
            return [.. GenerateData()];

            static IEnumerable<string> GenerateData()
            {
                string[] nameColonUsages =
                [
                    "",
                    "values: "
                ];

                string[] priorityNamedParameterUsages =
                [
                    "",
                    ", Priority = 1"
                ];

                string[] attributeUsagesBase =
                [
                    "[Arguments({1}{2})]",
                    "[Arguments({0}new object[] {{ {1} }}{2})]",
                    "[Arguments({0}[ {1} ]{2})]"
                ];

                string[] valueLists =
                [
                    "42, \"test\"",
                    "\"value\", 100"
                ];

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                        {
                            yield return string.Join("\n    ", valueLists.Select(vv => string.Format(attributeUsageBase, nameColonUsage, vv, priorityNamedParameterUsage)));
                        }
                    }
                }
            }
        }

        public static TheoryData<string> EmptyArgumentsAttributeUsagesWithLocationMarker()
        {
            return [.. GenerateData()];

            static IEnumerable<string> GenerateData()
            {
                yield return "[{|#0:Arguments|}]";
                yield return "[{|#0:Arguments()|}]";
                yield return "[{|#0:Arguments(Priority = 1)|}]";

                string[] nameColonUsages =
                [
                    "",
                    "values: "
                ];

                string[] priorityNamedParameterUsages =
                [
                    "",
                    ", Priority = 1"
                ];

                string[] attributeUsagesBase =
                [
                    "Arguments({0}new object[] {{ }}{1})",
                    "Arguments({0}new object[0]{1})",
                    "Arguments({0}[]{1})",
                ];

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                        {
                            yield return $"[{{|#0:{string.Format(attributeUsageBase, nameColonUsage, priorityNamedParameterUsage)}|}}]";
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> ArgumentsAttributeUsagesWithLocationMarker()
        {
            string[] nameColonUsages =
            [
                "",
                "values: "
            ];

            string[] priorityNamedParameterUsages =
            [
                "",
                ", Priority = 1"
            ];

            string[] attributeUsagesBase =
            [
                "Arguments({1}{2})",
                "Arguments({0}new object[] {{ {1} }}{2})",
                "Arguments({0}[ {1} ]{2})"
            ];

            string[] valueLists =
            [
                "42, \"test\"",
                "\"value\", 100, false"
            ];

            foreach (var attributeUsageBase in attributeUsagesBase)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                    {
                        yield return string.Join("\n    ", valueLists.Select((vv, i) => $"[{{|#{i}:{string.Format(attributeUsageBase, nameColonUsage, vv, priorityNamedParameterUsage)}|}}]"));
                    }
                }
            }
        }
    }

    public class MustHaveMatchingValueType : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
    {
        public MustHaveMatchingValueType() : base(ArgumentsAttributeAnalyzer.MustHaveMatchingValueTypeRule) { }

        [Fact]
        public async Task A_method_not_annotated_with_any_arguments_attributes_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    public void BenchmarkMethod()
                    {
                                                           
                    }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Analyzing_an_arguments_attribute_should_not_throw_an_inconsistent_language_versions_exception(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "42")}}]
                    public void BenchmarkMethod(int a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            SetParseOptions(LanguageVersion.CSharp14);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Analyzing_an_arguments_attribute_should_not_throw_an_inconsistent_syntax_tree_features_exception(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "42")}}]
                    public void BenchmarkMethod(int a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            SetParseOptions(LanguageVersion.Default, true);

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(EmptyArgumentsAttributeUsagesWithMismatchingValueCount))]
        public async Task Having_a_mismatching_value_count_with_empty_argument_attribute_usages_should_not_trigger_diagnostic(string argumentsAttributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;
                public class BenchmarkClass
                {
                    [Benchmark]
                    {{argumentsAttributeUsage}}
                    public void BenchmarkMethod(string a)
                    {
                                                    
                    }
                }
                """;
            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Having_a_mismatching_value_count_with_nonempty_argument_attribute_usages_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            [CombinatorialValues("string a", "")] string parameters)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;
                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{string.Format(scalarValuesContainerAttributeArgument, """
                                                                                42, "test"
                                                                                """)}}]
                    [{{string.Format(scalarValuesContainerAttributeArgument, """
                                                                                "value", 100, true
                                                                                """)}}]
                    public void BenchmarkMethod({{parameters}})
                    {
                                                    
                    }
                }
                """;
            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unknown_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "dummy_literal, true")}}]
                    public void BenchmarkMethod(byte a, bool b)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unknown_type_in_typeof_expression_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "typeof(int), typeof(dummy_literal)")}}]
                    public void BenchmarkMethod(System.Type a, System.Type b)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}}]
                    public void BenchmarkMethod({{valueAndType.Value2}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_enum_value_type_using_not_fully_qualified_name_located_in_a_different_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            bool isNullable)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;

                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1")}}]
                    public void BenchmarkMethod(DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_enum_value_type_using_not_fully_qualified_name_located_in_same_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            bool isNullable)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;
                                                
                namespace DifferentNamespace;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1")}}]
                    public void BenchmarkMethod(DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_enum_value_type_array_using_not_fully_qualified_name_located_in_a_different_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ArrayValuesContainerAttributeArgumentEnumerableLocal))] string arrayValuesContainerAttributeArgument,
            bool isNullable)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                                                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}{{string.Format(arrayValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1", $"DummyEnumInDifferentNamespace{(isNullable ? "?" : "")}")}}]
                    public void BenchmarkMethod(DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();
            DisableCompilerDiagnostics();                   // Nullable struct arrays are not supported in attributes

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_enum_value_type_array_using_not_fully_qualified_name_located_in_same_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ArrayValuesContainerAttributeArgumentEnumerableLocal))] string arrayValuesContainerAttributeArgument,
            bool isNullable)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                namespace DifferentNamespace;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}{{string.Format(arrayValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1", $"DummyEnumInDifferentNamespace{(isNullable ? "?" : "")}")}}]
                    public void BenchmarkMethod(DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();
            DisableCompilerDiagnostics();                   // Nullable struct arrays are not supported in attributes

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_type_using_not_fully_qualified_name_located_in_same_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ValuesAndTypesInDifferentNamespace))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;
                                                
                namespace DifferentNamespace;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}}]
                    public void BenchmarkMethod({{valueAndType.Value2}} a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_type_using_not_fully_qualified_name_located_in_a_different_namespace_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ValuesAndTypesInDifferentNamespace))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                                                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}}]
                    public void BenchmarkMethod({{valueAndType.Value2}} a)
                    {
                                                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_integer_value_types_within_target_type_range_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(IntegerValuesAndTypesWithinTargetTypeRange))] ValueTupleDouble<string, string> integerValueAndType,
            bool explicitCast)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, $"{(explicitCast ? $"({integerValueAndType.Value2})" : "")}{integerValueAndType.Value1}")}}]
                    public void BenchmarkMethod({{integerValueAndType.Value2}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_null_to_nullable_struct_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NullableStructTypes))] string type,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "null")}}]
                    public void BenchmarkMethod({{type}}? a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_constant_value_type_should_not_trigger_diagnostic(
            bool useConstantFromOtherClass,
            bool useLocalConstant,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ConstantValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {valueAndType.Value2} _x = {(useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!)};" : "")}}
                                                
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!)}}]
                    public void BenchmarkMethod({{valueAndType.Value2}} a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            if (useConstantFromOtherClass)
            {
                ReferenceConstants($"{valueAndType.Value2!}", valueAndType.Value1!);
            }

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_expected_null_reference_constant_value_type_should_not_trigger_diagnostic(
            bool useConstantFromOtherClass,
            bool useLocalConstant,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NullReferenceConstantTypes))] string type,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {type}? _x = {(useConstantFromOtherClass ? "Constants.Value" : "null")};" : "")}}
                                                
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : "null")}}]
                    public void BenchmarkMethod({{type}} a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            if (useConstantFromOtherClass)
            {
                ReferenceConstants($"{type}?", "null");
            }

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_implicitly_convertible_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            [CombinatorialValues("(byte)42", "'c'")] string value)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, value)}}]
                    public void BenchmarkMethod(int a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_implicitly_convertible_array_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "new[] { 0, 1, 2 }")}}]
                    public void BenchmarkMethod(System.Span<int> a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Having_unknown_parameter_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "42, \"test\"")}}]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "43, \"test2\"")}}]
                    public void BenchmarkMethod(unkown a, string b)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unexpected_or_not_implicitly_convertible_value_type_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NotConvertibleValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            const string expectedArgumentType = "decimal";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{valueAndType.Value1}|}}")}}]
                    public void BenchmarkMethod({{expectedArgumentType}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            AddDefaultExpectedDiagnostic(valueAndType.Value1!, expectedArgumentType, valueAndType.Value2!);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unexpected_or_not_implicitly_convertible_constant_value_type_should_trigger_diagnostic(
            bool useConstantFromOtherClass,
            bool useLocalConstant,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NotConvertibleConstantValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            const string expectedArgumentType = "decimal";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {valueAndType.Value2} _x = {(useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1)};" : "")}}
                                                
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1)}|}}")}}]
                    public void BenchmarkMethod({{expectedArgumentType}} a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            if (useConstantFromOtherClass)
            {
                ReferenceConstants(valueAndType.Value2!, valueAndType.Value1!);
            }

            AddDefaultExpectedDiagnostic(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!, expectedArgumentType, valueAndType.Value2!);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_null_to_nonnullable_struct_value_type_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NullableStructTypes))] string type,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "{|#0:null|}")}}]
                    public void BenchmarkMethod({{type}} a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            AddDefaultExpectedDiagnostic("null", type, "null");

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_a_not_implicitly_convertible_array_value_type_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            [CombinatorialValues("System.Span<string>", "string[]")] string expectedArgumentType)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "{|#0:new[] { 0, 1, 2 }|}")}}]
                    public void BenchmarkMethod({{expectedArgumentType}} a)
                    {
                                                    
                    }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            AddDefaultExpectedDiagnostic("new[] { 0, 1, 2 }", expectedArgumentType, "int[]");

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_integer_value_types_not_within_target_type_range_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(IntegerValuesAndTypesNotWithinTargetTypeRange))] ValueTupleTriple<string, string, string> integerValueAndType)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Benchmark]
                    [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{integerValueAndType.Value1}|}}")}}]
                    public void BenchmarkMethod({{integerValueAndType.Value2}} a)
                    {

                    }
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();
            AddDefaultExpectedDiagnostic(integerValueAndType.Value1!, integerValueAndType.Value2!, integerValueAndType.Value3!);

            await RunAsync();
        }

        public static IEnumerable<string> DummyAttributeUsage
            => DummyAttributeUsageTheoryData;

        public static TheoryData<string> EmptyArgumentsAttributeUsagesWithMismatchingValueCount()
        {
            return [.. GenerateData()];

            static IEnumerable<string> GenerateData()
            {
                yield return "[Arguments]";
                yield return "[Arguments()]";
                yield return "[Arguments(Priority = 1)]";

                string[] nameColonUsages = ["", "values: "];

                string[] priorityNamedParameterUsages = ["", ", Priority = 1"];

                string[] attributeUsagesBase =
                [
                    "[Arguments({0}new object[] {{ }}{1})]",
                    "[Arguments({0}new object[0]{1})]",
                    "[Arguments({0}[]{1})]",
                ];

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                        {
                            yield return string.Format(attributeUsageBase, nameColonUsage, priorityNamedParameterUsage);
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> ScalarValuesContainerAttributeArgumentEnumerableLocal
            => ScalarValuesContainerAttributeArgumentEnumerable();

        public static IEnumerable<ValueTupleDouble<string, string>> IntegerValuesAndTypesWithinTargetTypeRange =>
        [
            // byte (0 to 255)
            ("0", "byte"),
            ("100", "byte"),
            ("255", "byte"),

            // sbyte (-128 to 127)
            ("-128", "sbyte"),
            ("0", "sbyte"),
            ("127", "sbyte"),

            // short (-32,768 to 32,767)
            ("-32768", "short"),
            ("0", "short"),
            ("32767", "short"),

            // ushort (0 to 65,535)
            ("0", "ushort"),
            ("1000", "ushort"),
            ("65535", "ushort"),

            // int (-2,147,483,648 to 2,147,483,647)
            ("-2147483648", "int"),
            ("0", "int"),
            ("2147483647", "int"),

            // uint (0 to 4,294,967,295)
            ("0", "uint"),
            ("1000000", "uint"),
            ("4294967295", "uint"),

            // long (-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807)
            ("-9223372036854775808", "long"),
            ("0", "long"),
            ("9223372036854775807", "long"),

            // ulong (0 to 18,446,744,073,709,551,615)
            ("0", "ulong"),
            ("1000000", "ulong"),
            ("18446744073709551615", "ulong"),
        ];

        public static IEnumerable<ValueTupleTriple<string, string, string>> IntegerValuesAndTypesNotWithinTargetTypeRange =>
        [
            // byte (0 to 255) - out of range values
            ("-1", "byte", "int"),
            ("256", "byte", "int"),
            ("1000", "byte", "int"),

            // sbyte (-128 to 127) - out of range values
            ("-129", "sbyte", "int"),
            ("128", "sbyte", "int"),
            ("500", "sbyte", "int"),

            // short (-32,768 to 32,767) - out of range values
            ("-32769", "short", "int"),
            ("32768", "short", "int"),
            ("100000", "short", "int"),

            // ushort (0 to 65,535) - out of range values
            ("-1", "ushort", "int"),
            ("65536", "ushort", "int"),
            ("100000", "ushort", "int"),

            // int (-2,147,483,648 to 2,147,483,647) - out of range values
            ("-2147483649", "int", "long"),
            ("2147483648", "int", "uint"),
            ("5000000000", "int", "long"),

            // uint (0 to 4,294,967,295) - out of range values
            ("-1", "uint", "int"),
            ("4294967296", "uint", "long"),
            ("5000000000", "uint", "long"),

            // long - out of range values (exceeding long range)
            ("9223372036854775808", "long", "ulong"),

            // ulong - negative values
            ("-1", "ulong", "int"),
            ("-100", "ulong", "int"),
            ("-9223372036854775808", "ulong", "long"),
        ];

        public static IEnumerable<string> ArrayValuesContainerAttributeArgumentEnumerableLocal()
        {
            string[] nameColonUsages =
            [
                "",
                "values: "
            ];

            string[] priorityNamedParameterUsages =
            [
                "",
                ", Priority = 1"
            ];

            List<string> arrayValuesContainers =
            [
                "{0}new object[] {{{{ new[] {{{{ {{0}} }}}} }}}}{1}",         // new object[] { new[] { 42 } }
                "{0}new object[] {{{{ new {{1}}[] {{{{ {{0}} }}}} }}}}{1}",   // new object[] { new int[] { 42 } }
                "{0}[ new[] {{{{ {{0}} }}}} ]{1}",                            // [ new[] { 42 } ]
                "{0}[ new {{1}}[] {{{{ {{0}} }}}} ]{1}",                      // [ new int[] { 42 } ]
                "{0}new object[] {{{{ new {{1}}[] {{{{ }}}} }}}}{1}",         // new object[] { new int[] { } }
                "{0}[ new {{1}}[] {{{{ }}}} ]{1}",                            // [ new int[] { } ]
                "{0}new object[] {{{{ new {{1}}[0] }}}}{1}",                  // new object[] { new int[0] }
                "{0}[ new {{1}}[0] ]{1}"                                      // [ new int[0] ]
            ];

            foreach (var arrayValuesContainer in arrayValuesContainers)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                    {
                        yield return string.Format(arrayValuesContainer, nameColonUsage, priorityNamedParameterUsage);
                    }
                }
            }
        }

        public static IEnumerable<ValueTupleDouble<string, string>> ValuesAndTypes =>
        [
            ( "true", "bool" ),
            ( "(byte)123", "byte" ),
            ( "'A'", "char" ),
            ( "1.0D", "double" ),
            ( "1.0F", "float" ),
            ( "123", "int" ),
            ( "123L", "long" ),
            ( "(sbyte)-100", "sbyte" ),
            ( "(short)-123", "short" ),
            ( """
              "test"
              """, "string" ),
            ( "123U", "uint" ),
            ( "123UL", "ulong" ),
            ( "(ushort)123", "ushort" ),

            ( """
              (object)"test_object"
              """, "object" ),
            ( "typeof(string)", "System.Type" ),
            ( "typeof(DummyEnumInDifferentNamespace?)", "System.Type" ),
            ( "DummyEnum.Value1", "DummyEnum" ),

            ( "new[] { 0, 1, 2 }", "int[]" ),
        ];

        public static IEnumerable<ValueTupleDouble<string, string>> ValuesAndTypesInDifferentNamespace =>
        [
            ( "typeof(DummyEnumInDifferentNamespace)", "System.Type" ),
            ( "typeof(DummyEnumInDifferentNamespace?)", "System.Type" ),
            ( "DummyEnumInDifferentNamespace.Value1", "DummyEnumInDifferentNamespace" ),
        ];

        public static IEnumerable<ValueTupleDouble<string, string>> NotConvertibleValuesAndTypes =>
        [
            ( "true", "bool" ),
            ( "1.0D", "double" ),
            ( "1.0F", "float" ),
            ( """
              "test"
              """, "string" ),

            ( """
              (object)"test_object"
              """, "string" ),
            ( "typeof(string)", "System.Type" ),
            ( "DummyEnum.Value1", "DummyEnum" )
        ];

        public static IEnumerable<ValueTupleDouble<string, string>> ConstantValuesAndTypes =>
        [
            ( "true", "bool" ),
            ( "(byte)123", "byte" ),
            ( "'A'", "char" ),
            ( "1.0D", "double" ),
            ( "1.0F", "float" ),
            ( "123", "int" ),
            ( "123L", "long" ),
            ( "(sbyte)-100", "sbyte" ),
            ( "(short)-123", "short" ),
            ( """
              "test"
              """, "string" ),
            ( "123U", "uint" ),
            ( "123UL", "ulong" ),
            ( "(ushort)123", "ushort" ),

            ( "DummyEnum.Value1", "DummyEnum" ),
        ];

        public static IEnumerable<string> NullableStructTypes =>
        [
            "bool",
            "byte",
            "char",
            "double",
            "float",
            "int",
            "long",
            "sbyte",
            "short",
            "uint",
            "ulong",
            "ushort",
            "DummyEnum",
        ];

        public static IEnumerable<string> NullReferenceConstantTypes =>
        [
            "object",
            "string",
            "System.Type",
        ];

        public static IEnumerable<ValueTupleDouble<string, string>> NotConvertibleConstantValuesAndTypes =>
        [
            ( "true", "bool" ),
            ( "1.0D", "double" ),
            ( "1.0F", "float" ),
            ( """
              "test"
              """, "string" ),

            ( "DummyEnum.Value1", "DummyEnum" )
        ];
    }

    public static TheoryData<string> DummyAttributeUsageTheoryData => ["", "Dummy, "];

    private static IEnumerable<string> ScalarValuesContainerAttributeArgumentEnumerable()
    {
        return GenerateData().Distinct();

        static IEnumerable<string> GenerateData()
        {
            string[] nameColonUsages = ["", "values: "];

            string[] priorityNamedParameterUsages = ["", ", Priority = 1"];

            string[] attributeUsagesBase =
            [
                "Arguments({{0}}{1})",
                "Arguments({0}new object[] {{{{ {{0}} }}}}{1})",
                "Arguments({0}[ {{0}} ]{1})"
            ];

            foreach (var attributeUsageBase in attributeUsagesBase)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                    {
                        yield return string.Format(attributeUsageBase, nameColonUsage, priorityNamedParameterUsage);
                    }
                }
            }
        }
    }
}