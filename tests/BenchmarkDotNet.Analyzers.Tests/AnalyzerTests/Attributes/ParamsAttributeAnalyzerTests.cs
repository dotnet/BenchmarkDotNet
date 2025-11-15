using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;

public class ParamsAttributeAnalyzerTests
{
    public class General : AnalyzerTestFixture<ParamsAttributeAnalyzer>
    {
        [Theory, CombinatorialData]
        public async Task A_field_or_property_not_annotated_with_the_params_attribute_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialValues("", "[Dummy]")] string missingParamsAttributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{missingParamsAttributeUsage}}
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        public static IEnumerable<string> FieldOrPropertyDeclarations
#pragma warning disable IDE0028 // Simplify collection initialization
            => new FieldOrPropertyDeclarationsTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    public class MustHaveValues : AnalyzerTestFixture<ParamsAttributeAnalyzer>
    {
        public MustHaveValues() : base(ParamsAttributeAnalyzer.MustHaveValuesRule) { }

        [Theory, CombinatorialData]
        public async Task Providing_one_or_more_values_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesListLength))] int scalarValuesListLength,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, string.Join(", ", ScalarValues.Take(scalarValuesListLength)))}})]
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_array_with_a_rank_specifier_size_higher_than_zero_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialRange(1, 2)] int rankSpecifierSize)
        {
            Assert.True(rankSpecifierSize > 0);

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params(new object[{{rankSpecifierSize}}])]
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_no_values_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(EmptyParamsAttributeUsagesWithLocationMarker))] string emptyParamsAttributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}{{emptyParamsAttributeUsage}}]
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            AddDefaultExpectedDiagnostic();

            await RunAsync();
        }

        public static IEnumerable<string> FieldOrPropertyDeclarations
#pragma warning disable IDE0028 // Simplify collection initialization
            => new FieldOrPropertyDeclarationsTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization

        public static IEnumerable<string> DummyAttributeUsage
            => DummyAttributeUsageTheoryData;

        public static IEnumerable<int> ScalarValuesListLength
            => Enumerable.Range(1, ScalarValues.Count);

        private static ReadOnlyCollection<string> ScalarValues
            => Enumerable.Range(1, 3)
            .Select(i => $"\"test{i}\"")
            .ToList()
            .AsReadOnly();

        public static IEnumerable<string> ScalarValuesContainerAttributeArgument
            => ScalarValuesContainerAttributeArgumentTheoryData();

        public static IEnumerable<string> EmptyParamsAttributeUsagesWithLocationMarker()
        {
            yield return "{|#0:Params|}";
            yield return "Params{|#0:()|}";
            yield return "Params({|#0:Priority = 1|})";

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
                "Params({0}new object[] {{|#0:{{ }}|}}{1})",
                "Params({0}{{|#0:new object[0]|}}{1})",
                "Params({0}{{|#0:[ ]|}}{1})",
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

    public class MustHaveMatchingValueType : AnalyzerTestFixture<ParamsAttributeAnalyzer>
    {
        public MustHaveMatchingValueType() : base(ParamsAttributeAnalyzer.MustHaveMatchingValueTypeRule) { }

        [Theory, CombinatorialData]
        public async Task Analyzing_a_params_attribute_should_not_throw_an_inconsistent_language_versions_exception(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "42")}})]
                    public int {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            SetParseOptions(LanguageVersion.CSharp14);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Analyzing_a_params_attribute_should_not_throw_an_inconsistent_syntax_tree_features_exception(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "42")}})]
                    public int {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            SetParseOptions(LanguageVersion.Default, true);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_a_field_or_property_with_an_unknown_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "42, 51, 33")}})]
                    public unknown {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}})]
                    public {{valueAndType.Value2}} {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            bool isNullable,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;

                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1")}})]
                    public DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            bool isNullable,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                namespace DifferentNamespace;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1")}})]
                    public DummyEnumInDifferentNamespace{{(isNullable ? "?" : "")}} {{fieldOrPropertyDeclaration}}
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
            bool isNullable,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(arrayValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1", $"DummyEnumInDifferentNamespace{(isNullable ? "?" : "")}")}})]
                    public DummyEnumInDifferentNamespace[] {{fieldOrPropertyDeclaration}}
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
            bool isNullable,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                namespace DifferentNamespace;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(arrayValuesContainerAttributeArgument, "DummyEnumInDifferentNamespace.Value1", $"DummyEnumInDifferentNamespace{(isNullable ? "?" : "")}")}})]
                    public DummyEnumInDifferentNamespace[] {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                namespace DifferentNamespace;
                
                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}})]
                    public {{valueAndType.Value2}} {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, valueAndType.Value1)}})]
                    public {{valueAndType.Value2}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnumInDifferentNamespace();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_null_to_nullable_struct_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NullableStructTypes))] string type,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "null")}})]
                    public {{type}}? {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {valueAndType.Value2} _x = {(useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!)};" : "")}}
                                                
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!)}})]
                    public {{valueAndType.Value2}} {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {type}? _x = {(useConstantFromOtherClass ? "Constants.Value" : "null")};" : "")}}
                                                
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : "null")}})]
                    public {{type}}? {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialValues("(byte)42", "'c'")] string value,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, value)}})]
                    public int {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_integer_value_types_within_target_type_range_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(IntegerValuesAndTypesWithinTargetTypeRange))] ValueTupleDouble<string, string> integerValueAndType,
            bool explicitCast,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"{(explicitCast ? $"({integerValueAndType.Value2})" : "")}{integerValueAndType.Value1}")}})]
                    public {{integerValueAndType.Value2}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unknown_value_type_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "42, dummy_literal, 33")}})]
                    public int {{fieldOrPropertyDeclaration}}
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
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "typeof(int), typeof(dummy_literal)")}})]
                    public System.Type {{fieldOrPropertyDeclaration}}
                }
                """;
            TestCode = testCode;
            ReferenceDummyAttribute();

            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_both_expected_and_unexpected_value_types_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            const string expectedFieldOrPropertyType = "int";
            const string valueWithUnexpectedType = "\"test1\"";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"42, {{|#0:{valueWithUnexpectedType}|}}, 33")}})]
                    public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            AddDefaultExpectedDiagnostic(valueWithUnexpectedType, expectedFieldOrPropertyType, "string");

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unexpected_or_not_implicitly_convertible_value_type_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NotConvertibleValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            const string expectedFieldOrPropertyType = "decimal";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{valueAndType.Value1}|}}")}})]
                    public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            AddDefaultExpectedDiagnostic(valueAndType.Value1!, expectedFieldOrPropertyType, valueAndType.Value2!);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unexpected_or_not_implicitly_convertible_constant_value_type_should_trigger_diagnostic(
            bool useConstantFromOtherClass,
            bool useLocalConstant,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NotConvertibleConstantValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            const string expectedFieldOrPropertyType = "decimal";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{(useLocalConstant ? $"private const {valueAndType.Value2} _x = {(useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1)};" : "")}}
                                                
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1)}|}}")}})]
                    public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            if (useConstantFromOtherClass)
            {
                ReferenceConstants(valueAndType.Value2!, valueAndType.Value1!);
            }

            AddDefaultExpectedDiagnostic(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : valueAndType.Value1!, expectedFieldOrPropertyType, valueAndType.Value2!);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_null_to_nonnullable_struct_value_type_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(NullableStructTypes))] string type,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "{|#0:null|}")}})]
                    public {{type}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();

            AddDefaultExpectedDiagnostic("null", type, "null");

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_integer_value_types_not_within_target_type_range_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerable))] string scalarValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(IntegerValuesAndTypesNotWithinTargetTypeRange))] ValueTupleTriple<string, string, string> integerValueAndType,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{integerValueAndType.Value1}|}}")}})]
                    public {{integerValueAndType.Value2}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            AddDefaultExpectedDiagnostic(integerValueAndType.Value1!, integerValueAndType.Value2!, integerValueAndType.Value3!);

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_unexpected_array_value_type_to_params_attribute_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
            [CombinatorialMemberData(nameof(ArrayValuesContainerAttributeArgumentWithLocationMarkerEnumerableLocal))] ValueTupleDouble<string, string> arrayValuesContainerAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            const string expectedFieldOrPropertyType = "decimal";

            var testCode = /* lang=c#-test */ $$"""
                using DifferentNamespace;
                
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(arrayValuesContainerAttributeArgument.Value1!, valueAndType.Value1, valueAndType.Value2)}})]
                    public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            ReferenceDummyEnum();
            ReferenceDummyEnumInDifferentNamespace();

            AddDefaultExpectedDiagnostic(
                string.Format(arrayValuesContainerAttributeArgument.Value2!, valueAndType.Value1, valueAndType.Value2),
                expectedFieldOrPropertyType,
                $"{valueAndType.Value2}[]"
            );

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_empty_array_value_when_type_of_attribute_target_is_not_object_array_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(EmptyValuesAttributeArgument))] string emptyValuesAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params{{emptyValuesAttributeArgument}}]
                    public decimal {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_an_empty_array_value_when_type_of_attribute_target_is_object_array_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(EmptyValuesAttributeArgument))] string emptyValuesAttributeArgument,
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params{{emptyValuesAttributeArgument}}]
                    public object[] {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        public static IEnumerable<string> FieldOrPropertyDeclarations
#pragma warning disable IDE0028 // Simplify collection initialization
            => new FieldOrPropertyDeclarationsTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization

        public static IEnumerable<string> DummyAttributeUsage
            => DummyAttributeUsageTheoryData;

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

        public static IEnumerable<string> EmptyValuesAttributeArgument()
        {
            yield return "";
            yield return "()";
            yield return "(Priority = 1)";

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
                "({0}new object[] {{ }}{1})",
                "({0}new object[0]{1})",
                "({0}[ ]{1})"
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

        public static IEnumerable<string> ScalarValuesContainerAttributeArgumentEnumerable
            => ScalarValuesContainerAttributeArgumentTheoryData();

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

        public static IEnumerable<ValueTupleDouble<string, string>> ArrayValuesContainerAttributeArgumentWithLocationMarkerEnumerableLocal()
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

            (string, string)[] arrayValuesContainers =
            [
                ("{0}new object[] {{{{ {{{{|#0:new[] {{{{ {{0}} }}}}|}}}} }}}}{1}",       "new[] {{ {0} }}"),       // new object[] { new[] { 42 } }
                ("{0}new object[] {{{{ {{{{|#0:new {{1}}[] {{{{ {{0}} }}}}|}}}} }}}}{1}", "new {1}[] {{ {0} }}"),   // new object[] { new int[] { 42 } }
                ("{0}[ {{{{|#0:new[] {{{{ {{0}} }}}}|}}}} ]{1}",                          "new[] {{ {0} }}"),       // [ new[] { 42 } ]
                ("{0}[ {{{{|#0:new {{1}}[] {{{{ {{0}} }}}}|}}}} ]{1}",                    "new {1}[] {{ {0} }}"),   // [ new int[] { 42 } ]
                ("{0}new object[] {{{{ {{{{|#0:new {{1}}[] {{{{ }}}}|}}}} }}}}{1}",       "new {1}[] {{ }}"),       // new object[] { new int[] { } }
                ("{0}[ {{{{|#0:new {{1}}[] {{{{ }}}}|}}}} ]{1}",                          "new {1}[] {{ }}"),       // [ new int[] { } ]
                ("{0}new object[] {{{{ {{{{|#0:new {{1}}[0]|}}}} }}}}{1}",                "new {1}[0]"),            // new object[] { new int[0] }
                ("{0}[ {{{{|#0:new {{1}}[0]|}}}} ]{1}",                                   "new {1}[0]"),            // [ new int[0] ]
            ];

            foreach (var arrayValuesContainer in arrayValuesContainers)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                    {
                        yield return new ValueTupleDouble<string, string>
                        {
                            Value1 = string.Format(arrayValuesContainer.Item1, nameColonUsage, priorityNamedParameterUsage),
                            Value2 = arrayValuesContainer.Item2
                        };
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
              """, "object" ),
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

    public class UnnecessarySingleValuePassedToAttribute : AnalyzerTestFixture<ParamsAttributeAnalyzer>
    {
        public UnnecessarySingleValuePassedToAttribute() : base(ParamsAttributeAnalyzer.UnnecessarySingleValuePassedToAttributeRule) { }

        [Theory, CombinatorialData]
        public async Task Providing_two_or_more_values_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesListLength))] int scalarValuesListLength,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, string.Join(", ", ScalarValues.Take(scalarValuesListLength)))}})]
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task Providing_only_a_single_value_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
            [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
            [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentWithLocationMarker))] string scalarValuesContainerAttributeArgument)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, 42)}})]
                    public string {{fieldOrPropertyDeclaration}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            AddDefaultExpectedDiagnostic();

            await RunAsync();
        }

        public static IEnumerable<string> FieldOrPropertyDeclarations
#pragma warning disable IDE0028 // Simplify collection initialization
            => new FieldOrPropertyDeclarationsTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization

        public static IEnumerable<string> DummyAttributeUsage
            => DummyAttributeUsageTheoryData;

        public static IEnumerable<int> ScalarValuesListLength
            => Enumerable.Range(2, ScalarValues.Count);

        private static ReadOnlyCollection<string> ScalarValues
            => Enumerable.Range(1, 2)
            .Select(i => $"\"test{i}\"")
            .ToList()
            .AsReadOnly();
        public static IEnumerable<string> ScalarValuesContainerAttributeArgument
            => ScalarValuesContainerAttributeArgumentTheoryData();

        public static IEnumerable<string> ScalarValuesContainerAttributeArgumentWithLocationMarker()
        {
            return GenerateData().Distinct();

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
                    "{{{{|#0:{{0}}|}}}}{1}",
                    "{0}new object[] {{{{ {{{{|#0:{{0}}|}}}} }}}}{1}",
                    "{0}[ {{{{|#0:{{0}}|}}}} ]{1}",
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

    public static TheoryData<string> DummyAttributeUsageTheoryData => ["", "Dummy, "];

    public static TheoryData<string> ScalarValuesContainerAttributeArgumentTheoryData()
    {
        return [.. GenerateData().Distinct()];

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
                "{{0}}{1}",
                "{0}new object[] {{{{ {{0}} }}}}{1}",
                "{0}[ {{0}} ]{1}"
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