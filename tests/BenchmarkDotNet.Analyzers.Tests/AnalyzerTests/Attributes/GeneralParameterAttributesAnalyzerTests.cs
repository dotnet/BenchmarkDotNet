using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Analyzers.Attributes;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using Microsoft.CodeAnalysis;
using System.Collections.ObjectModel;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes;

public class GeneralParameterAttributesAnalyzerTests
{
    public class MutuallyExclusiveOnField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public MutuallyExclusiveOnField() : base(GeneralParameterAttributesAnalyzer.MutuallyExclusiveOnFieldRule) { }

        [Fact]
        public async Task A_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task A_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Fact]
        public async Task A_field_annotated_with_a_duplicate_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    [Dummy]
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_field_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
        public async Task A_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(string currentUniqueAttributeUsage, int currentUniqueAttributeUsagePosition, int[] counts)
        {
            var duplicateAttributeUsages = new List<string>(1 + counts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < counts.Length; i++)
            {
                if (i == currentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{currentUniqueAttributeUsage}]");
                }

                for (var j = 0; j < counts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateParameterAttributeUsageCounts))]
        public async Task A_field_annotated_with_more_than_one_parameter_attribute_should_trigger_diagnostic_for_each_attribute_usage(int[] duplicateAttributeUsageCounts)
        {
            const string fieldIdentifier = "_field";

            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            var diagnosticCounter = 0;
            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{{|#{diagnosticCounter++}:{uniqueParameterAttributeUsages[i]}|}}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int {{fieldIdentifier}} = 0, field2 = 1;
                }
                """;

            TestCode = testCode;

            for (var i = 0; i < diagnosticCounter; i++)
            {
                AddExpectedDiagnostic(i, fieldIdentifier);
            }

            await RunAsync();
        }

        // Two *different* classes from one parameter-attribute family. The compiler says nothing about them -
        // CS0579 covers only the same class applied twice - so the duplicate has to be reported here. This also
        // guards the whole method: the duplicate check used to bail out on this shape, taking every other
        // diagnostic for the member with it, so a private field carrying them compiled clean.
        [Fact]
        public async Task A_field_annotated_with_two_different_classes_from_one_family_should_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class CustomParamsAttribute : ParamsAttribute
                {
                    public CustomParamsAttribute(params object[] values) : base(values) { }
                }

                public class BenchmarkClass
                {
                    [{|#0:Params(1)|}]
                    [{|#1:CustomParams(2)|}]
                    private int _field = 0;
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, "_field");
            AddExpectedDiagnostic(1, "_field");
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData;

        public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;
    }

    public class MutuallyExclusiveOnProperty : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public MutuallyExclusiveOnProperty() : base(GeneralParameterAttributesAnalyzer.MutuallyExclusiveOnPropertyRule) { }

        [Fact]
        public async Task A_property_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task A_property_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Fact]
        public async Task A_property_annotated_with_a_duplicate_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    [Dummy]
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_property_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
        public async Task A_property_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            string currentAttributeUsage,
            int currentUniqueAttributeUsagePosition,
            int[] duplicateSameAttributeUsageCounts)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameAttributeUsageCounts.Length; i++)
            {
                if (i == currentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{currentAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateParameterAttributeUsages))]
        public async Task A_property_annotated_with_more_than_one_parameter_attribute_should_trigger_diagnostic_for_each_attribute_usage(int[] duplicateAttributeUsageCounts)
        {
            const string propertyIdentifier = "Property";

            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            var diagnosticCounter = 0;
            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{{|#{diagnosticCounter++}:{uniqueParameterAttributeUsages[i]}|}}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int {{propertyIdentifier}} { get; set; }
                }
                """;

            TestCode = testCode;

            for (var i = 0; i < diagnosticCounter; i++)
            {
                AddExpectedDiagnostic(i, propertyIdentifier);
            }

            await RunAsync();
        }

        public static TheoryData<string> UniqueParameterAttributeUsages => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData;

        public static TheoryData<int[]> DuplicateParameterAttributeUsages => DuplicateAttributeUsageCountsTheoryData;
    }

    public class FieldMustBePublic : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public FieldMustBePublic() : base(GeneralParameterAttributesAnalyzer.FieldMustBePublic) { }

        [Theory]
        [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
        public async Task A_nonpublic_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    {{classMemberAccessModifier}}int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
        public async Task A_nonpublic_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    [Dummy]
                    {{classMemberAccessModifier}}int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_public_field_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int _field = 0, _field2 = 2;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_nonpublic_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
            [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameParameterAttributeUsages.Counts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameParameterAttributeUsages.Counts.Length; i++)
            {
                if (i == duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameParameterAttributeUsages.Counts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    {{classMemberAccessModifier}}int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations))]
        public async Task A_nonpublic_field_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts, string classMemberAccessModifier)
        {
            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    {{classMemberAccessModifier}}int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_nonpublic_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
            [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
        {
            const string fieldIdentifier = "_field";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attribute.AttributeUsage}}]
                    {{classMemberAccessModifier}}int {|#0:{{fieldIdentifier}}|} = 0, field2 = 0;
                }
                """;
            TestCode = testCode;
            AddDefaultExpectedDiagnostic(fieldIdentifier, attribute.AttributeName);

            await RunAsync();
        }

        public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations
            => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicClassMemberAccessModifiers);

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes
            => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));

        public static IEnumerable<string> NonPublicClassMemberAccessModifiers
#pragma warning disable IDE0028 // Simplify collection initialization
            => new NonPublicClassMemberAccessModifiersTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization

        public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));

        public static IEnumerable<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;
    }

    public class PropertyMustBePublic : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public PropertyMustBePublic() : base(GeneralParameterAttributesAnalyzer.PropertyMustBePublic) { }

        [Theory]
        [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
        public async Task A_nonpublic_property_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    {{classMemberAccessModifier}}int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
        public async Task A_nonpublic_property_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    [Dummy]
                    {{classMemberAccessModifier}}int Property { get; set; }
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_public_property_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_nonpublic_property_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
            [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameParameterAttributeUsages.Counts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameParameterAttributeUsages.Counts.Length; i++)
            {
                if (i == duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameParameterAttributeUsages.Counts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    {{classMemberAccessModifier}}int Property { get; set; }
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations))]
        public async Task A_nonpublic_property_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts, string classMemberAccessModifier)
        {
            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    {{classMemberAccessModifier}}int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_nonpublic_property_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
            [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
        {
            const string propertyIdentifier = "Property";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attribute.AttributeUsage}}]
                    {{classMemberAccessModifier}}int {|#0:{{propertyIdentifier}}|} { get; set; }
                }
                """;
            TestCode = testCode;
            AddDefaultExpectedDiagnostic(propertyIdentifier, attribute.AttributeName);

            await RunAsync();
        }

        public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations
            => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicClassMemberAccessModifiers);

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes
            => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));

        public static IEnumerable<string> NonPublicClassMemberAccessModifiers
#pragma warning disable IDE0028 // Simplify collection initialization
            => new NonPublicClassMemberAccessModifiersTheoryData();
#pragma warning restore IDE0028 // Simplify collection initialization

        public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));

        public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;
    }

    public class NotValidOnReadonlyField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public NotValidOnReadonlyField() : base(GeneralParameterAttributesAnalyzer.NotValidOnReadonlyFieldRule) { }

        [Fact]
        public async Task A_readonly_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    public readonly int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task A_readonly_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    public readonly int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_field_without_a_readonly_modifier_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
        public async Task A_readonly_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            string currentAttributeUsage,
            int currentUniqueAttributeUsagePosition,
            int[] duplicateSameAttributeUsageCounts)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameAttributeUsageCounts.Length; i++)
            {
                if (i == currentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{currentAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public readonly int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateParameterAttributeUsageCounts))]
        public async Task A_readonly_field_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts)
        {
            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public readonly int _field = 0, _field2 = 1;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributes))]
        public async Task A_readonly_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(string attributeName, string attributeUsage)
        {
            const string fieldIdentifier = "_field";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public {|#0:readonly|} int {{fieldIdentifier}} = 0, field2 = 1;
                }
                """;
            TestCode = testCode;
            AddDefaultExpectedDiagnostic(fieldIdentifier, attributeName);

            await RunAsync();
        }

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static TheoryData<string, string> UniqueParameterAttributes
            => UniqueParameterAttributesTheoryData;

        public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData;

        public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;
    }

    public class NotValidOnConstantField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public NotValidOnConstantField() : base(GeneralParameterAttributesAnalyzer.NotValidOnConstantFieldRule) { }

        [Fact]
        public async Task A_constant_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    public const int Constant = 0;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Fact]
        public async Task A_constant_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
        {
            const string testCode = /* lang=c#-test */ """
                public class BenchmarkClass
                {
                    [Dummy]
                    public const int Constant = 0;
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
        public async Task A_constant_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            string currentAttributeUsage,
            int currentUniqueAttributeUsagePosition,
            int[] duplicateSameAttributeUsageCounts)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameAttributeUsageCounts.Sum());
            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameAttributeUsageCounts.Length; i++)
            {
                if (i == currentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{currentAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public const int Constant = 0;
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateParameterAttributeUsageCounts))]
        public async Task A_constant_field_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts)
        {
            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public const int Constant = 0;
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributes))]
        public async Task A_constant_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(string attributeName, string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public {|#0:const|} int Constant = 0;
                }
                """;
            TestCode = testCode;
            AddDefaultExpectedDiagnostic(attributeName);

            await RunAsync();
        }

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static TheoryData<string, string> UniqueParameterAttributes
            => UniqueParameterAttributesTheoryData;

        public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData;

        public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;
    }

    public class PropertyMustHavePublicSetter : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public PropertyMustHavePublicSetter() : base(GeneralParameterAttributesAnalyzer.PropertyMustHavePublicSetterRule) { }

        [Theory]
        [MemberData(nameof(NonPublicPropertySettersTheoryData))]
        public async Task A_property_with_a_nonpublic_setter_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic(string nonPublicPropertySetter)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    public int Property {{nonPublicPropertySetter}}
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(NonPublicPropertySettersTheoryData))]
        public async Task A_property_with_a_nonpublic_setter_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string nonPublicPropertySetter)
        {
            var testCode = /* lang=c#-test */ $$"""
                public class BenchmarkClass
                {
                    [Dummy]
                    public int Property {{nonPublicPropertySetter}}
                }
                """;

            TestCode = testCode;
            ReferenceDummyAttribute();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(UniqueParameterAttributeUsages))]
        public async Task A_property_with_an_assignable_setter_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attributeUsage}}]
                    public int Property { get; set; }
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_property_with_a_nonpublic_setter_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(
            [CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
            [CombinatorialMemberData(nameof(NonPublicPropertySetters))] string nonPublicPropertySetter)
        {
            var duplicateAttributeUsages = new List<string>(1 + duplicateSameParameterAttributeUsages.Counts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateSameParameterAttributeUsages.Counts.Length; i++)
            {
                if (i == duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsagePosition)
                {
                    duplicateAttributeUsages.Add($"[{duplicateSameParameterAttributeUsages.CurrentUniqueAttributeUsage}]");
                }

                for (var j = 0; j < duplicateSameParameterAttributeUsages.Counts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int Property {{nonPublicPropertySetter}}
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();

            await RunAsync();
        }

        [Theory]
        [MemberData(nameof(DuplicateAttributeUsageCountsAndNonPublicPropertySetterCombinations))]
        public async Task A_property_with_a_nonpublic_setter_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts, string nonPublicPropertySetter)
        {
            var duplicateAttributeUsages = new List<string>(duplicateAttributeUsageCounts.Sum());

            var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

            for (var i = 0; i < duplicateAttributeUsageCounts.Length; i++)
            {
                for (var j = 0; j < duplicateAttributeUsageCounts[i]; j++)
                {
                    duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                }
            }

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    {{string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}}
                    public int Property {{nonPublicPropertySetter}}
                }
                """;

            TestCode = testCode;

            await RunAsync();
        }

        [Theory, CombinatorialData]
        public async Task A_property_with_a_nonpublic_setter_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(
            [CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
            [CombinatorialMemberData(nameof(NonPublicPropertySetters))] string nonPublicPropertySetter)
        {
            const string propertyIdentifier = "Property";

            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [{{attribute.AttributeUsage}}]
                    public int {|#0:{{propertyIdentifier}}|} {{nonPublicPropertySetter}}
                }
                """;

            TestCode = testCode;
            AddDefaultExpectedDiagnostic(propertyIdentifier, attribute.AttributeName);

            await RunAsync();
        }

        public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicPropertySetterCombinations
            => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicPropertySetters());

        public static TheoryData<string> UniqueParameterAttributeUsages
            => [.. UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!)];

        public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes
            => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));

        public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages
            => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));

        public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts
            => DuplicateAttributeUsageCountsTheoryData;

        public static IEnumerable<string> NonPublicPropertySetters()
            => new NonPublicPropertySetterAccessModifiersTheoryData()
            .Select<string, string>(m => $"{{ get; {m} set; }}")
            .Concat(["{ get; }", "=> 0;"]);

        public static TheoryData<string> NonPublicPropertySettersTheoryData()
            => [.. NonPublicPropertySetters()];
    }

    public class ParamsSourceCannotUseWriteOnlyProperty : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public ParamsSourceCannotUseWriteOnlyProperty() : base(GeneralParameterAttributesAnalyzer.ParamsSourceCannotUseWriteOnlyPropertyRule) { }

        [Fact]
        public async Task UsingNameofWithWriteOnlyProperty_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    private int _value;

                    public int WriteOnlyProperty
                    {
                        set { _value = value; }
                    }

                    [ParamsSource({|#0:nameof(WriteOnlyProperty)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "WriteOnlyProperty");

            await RunAsync();
        }

        [Fact]
        public async Task UsingStringLiteralWithWriteOnlyProperty_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    private int _value;

                    public int WriteOnlyProperty
                    {
                        set { _value = value; }
                    }

                    [ParamsSource({|#0:"WriteOnlyProperty"|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "WriteOnlyProperty");

            await RunAsync();
        }

        [Fact]
        public async Task UsingTwoParameterConstructorWithWriteOnlyProperty_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class OtherClass
                {
                    private int _value;

                    public int WriteOnlyProperty
                    {
                        set { _value = value; }
                    }
                }

                public class BenchmarkClass
                {
                    [ParamsSource(typeof(OtherClass), {|#0:nameof(OtherClass.WriteOnlyProperty)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "WriteOnlyProperty");

            await RunAsync();
        }

        [Fact]
        public async Task UsingNameofWithReadWriteProperty_ShouldNotReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static int[] ValidValues { get; set; } = new[] { 1, 2, 3 };

                    [ParamsSource(nameof(ValidValues))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task UsingNameofWithReadOnlyProperty_ShouldNotReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<int> ValidValues => new[] { 1, 2, 3 };

                    [ParamsSource(nameof(ValidValues))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class ParamsSourceMustReturnEnumerable : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public ParamsSourceMustReturnEnumerable() : base(GeneralParameterAttributesAnalyzer.ParamsSourceMustReturnEnumerableRule) { }

        [Theory]
        [InlineData("System.Collections.Generic.IEnumerable<int> Values() => null;")]
        [InlineData("int[] Values() => null;")]
        [InlineData("System.Collections.Generic.List<int> Values() => null;")]
        [InlineData("System.Collections.Generic.IAsyncEnumerable<int> Values() => null;")]
        [InlineData("System.Collections.Generic.IEnumerable<int> Values => null;")]
        public async Task SupportedReturnType_ShouldNotReportError(string sourceMember)
        {
            var testCode = /* lang=c#-test */ $$"""
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static {{sourceMember}}

                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task CustomAsyncEnumerablePattern_ShouldReportError()
        {
            // The await-foreach pattern without the IAsyncEnumerable<T> interface is not supported.
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using BenchmarkDotNet.Attributes;

                public sealed class CustomAsyncEnumerable
                {
                    public CustomAsyncEnumerator GetAsyncEnumerator() => new();
                }

                public sealed class CustomAsyncEnumerator
                {
                    public int Current => 0;
                    public ValueTask<bool> MoveNextAsync() => new(false);
                }

                public class BenchmarkClass
                {
                    public static CustomAsyncEnumerable Values() => new();

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "CustomAsyncEnumerable");
            // ValueTask isn't resolvable in the net472 test compilation; the analyzer only inspects the source's
            // declared return type, so the compiler diagnostics are irrelevant here (matches SupportedReturnType).
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_derived_attribute_naming_its_source_through_the_base_constructor_does_not_report_error()
        {
            // The runtime reads the Name property, which base(...) set. This usage's own argument is a label that
            // happens to match a real member, so reading it as the source name resolves the wrong one and reports
            // against a member the benchmark never uses.
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class LabelledParamsSourceAttribute : ParamsSourceAttribute
                {
                    public LabelledParamsSourceAttribute(string label) : base("Values") { }
                }

                public class BenchmarkClass
                {
                    public int Label => 0;

                    public static IEnumerable<int> Values() => null;

                    [LabelledParamsSource("Label")]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task NonEnumerableReturnType_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static int Values() => 0;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "int");

            await RunAsync();
        }

        [Theory]
        [InlineData("System.Collections.IEnumerable Values() => null;", "System.Collections.IEnumerable")]
        [InlineData("System.Collections.ArrayList Values() => null;", "System.Collections.ArrayList")]
        public async Task NonGenericEnumerableReturnType_ShouldReportError(string sourceMember, string returnTypeName)
        {
            // The generated code infers the element type from the source, so a type that only implements the
            // non-generic IEnumerable gives inference nothing to bind to and fails to compile (CS0411).
            var testCode = /* lang=c#-test */ $$"""
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static {{sourceMember}}

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", returnTypeName);
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task TaskOfEnumerableReturnType_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static Task<IEnumerable<int>> Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            DisableCompilerDiagnostics();
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<int>>");

            await RunAsync();
        }

        [Fact]
        public async Task NonEnumerableReturnType_FromBaseClass_ShouldReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BaseClass
                {
                    public static int Values() => 0;
                }

                public class BenchmarkClass : BaseClass
                {
                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "int");

            await RunAsync();
        }

        [Fact]
        public async Task EnumerableReturnType_OnParameterlessOverload_ShouldNotReportError()
        {
            // The overload with a required parameter is not the one BDN invokes, so its return type is irrelevant.
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static int Values(int count) => count;
                    public static IEnumerable<int> Values() => new[] { 1, 2, 3 };

                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task EnumerableReturnType_FromBaseClass_ShouldNotReportError()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BaseClass
                {
                    public static IEnumerable<int> Values() => new[] { 1, 2, 3 };
                }

                public class BenchmarkClass : BaseClass
                {
                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;

            TestCode = testCode;
            await RunAsync();
        }
    }

    public class ReservedMemberName : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public ReservedMemberName() : base(GeneralParameterAttributesAnalyzer.ReservedMemberNameRule) { }

        [Fact]
        public async Task A_params_property_named_like_a_generated_member_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int {|#0:__GlobalSetup|} { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "__GlobalSetup", "Params");
            await RunAsync();
        }

        [Fact]
        public async Task A_params_field_named_like_a_generated_member_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int {|#0:__fieldsContainer|} = 0;

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "__fieldsContainer", "Params");
            await RunAsync();
        }

        [Fact]
        public async Task A_params_source_property_named_like_a_generated_member_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<int> Values() => new[] { 1, 2, 3 };

                    [ParamsSource(nameof(Values))]
                    public int {|#0:__WorkloadActionUnroll|} { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "__WorkloadActionUnroll", "ParamsSource");
            await RunAsync();
        }

        [Theory]
        [InlineData("__Run")]              // the generated runnable's static entry-point method
        [InlineData("__FieldsContainer")]  // the generated nested arguments struct
        public async Task A_params_member_named_like_a_generated_entry_point_or_container_reports_error(string name)
        {
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int {|#0:{{name}}|} { get; set; }

                    [Benchmark]
                    public void Run2() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, name, "Params");
            await RunAsync();
        }

        [Theory]
        [InlineData("OverheadActionUnroll")]
        [InlineData("Run")]              // the entry point is __Run, so plain Run is the benchmark's to use
        [InlineData("FieldsContainer")]  // likewise the arguments struct is __FieldsContainer
        public async Task A_params_member_named_like_a_freed_template_name_does_not_report_error(string name)
        {
            // The un-prefixed template names are free to use now that generated members are __-prefixed. #2821
            var testCode = /* lang=c#-test */ $$"""
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int {{name}} { get; set; }

                    [Benchmark]
                    public void Benchmark() { }
                }
                """;
            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task A_non_parameter_member_named_like_a_generated_member_does_not_report_error()
        {
            // Non-parameter members are reached via hiding, not an object initializer, so they don't collide.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public int __GlobalSetup { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            await RunAsync();
        }
    }

    public class ParamsSourceMethodRequiresOptionalParameters : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public ParamsSourceMethodRequiresOptionalParameters() : base(AnalyzerHelper.SourceMethodMustNotHaveRequiredParametersRule) { }

        [Fact]
        public async Task A_source_method_with_a_required_parameter_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<int> Values(int count) => new[] { count };

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values");
            await RunAsync();
        }

        [Fact]
        public async Task A_source_method_with_only_optional_parameters_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<int> Values(int count = 1) => new[] { count };

                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task An_overload_with_required_parameters_does_not_report_error_when_a_parameterless_one_exists()
        {
            // BDN invokes the parameterless overload, so the source is valid.
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<int> Values(int count) => new[] { count };
                    public static IEnumerable<int> Values() => new[] { 1 };

                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            await RunAsync();
        }
    }

    public class ReservedNameAcrossDeclarators : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public ReservedNameAcrossDeclarators() : base(GeneralParameterAttributesAnalyzer.ReservedMemberNameRule) { }

        [Fact]
        public async Task AReservedNameOnALaterDeclarator_ShouldReportError()
        {
            // One declaration, several members - the attribute applies to each, and the runnable's object
            // initializer has to bind each. Checking only the first leaves the rest unreported.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int Fine, {|#0:__Overhead|};

                    [Benchmark]
                    public int Run() => Fine;
                }
                """;

            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "__Overhead", "Params");
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }

    public class SourceElementMustNotBeByRefLike : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public SourceElementMustNotBeByRefLike() : base(AnalyzerHelper.SourceElementMustNotBeByRefLikeRule) { }

        [Fact]
        public async Task A_source_yielding_a_ref_struct_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System;
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IEnumerable<Span<int>> Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "System.Span<int>", "is a ref struct");
            DisableCompilerDiagnostics();
            await RunAsync();
        }


        // On the derived type the argument is fixed, so the constraint stops deciding anything.
        [Fact]
        public async Task A_source_closed_by_the_derived_type_to_a_value_type_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public abstract class BaseClass<T> where T : allows ref struct
                {
                    public static IEnumerable<T> Values() => null;
                }

                public class BenchmarkClass : BaseClass<int>
                {
                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_source_closed_by_the_derived_type_to_a_ref_struct_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System;
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public abstract class BaseClass<T> where T : allows ref struct
                {
                    public static IEnumerable<T> Values() => null;
                }

                public class BenchmarkClass : BaseClass<ReadOnlySpan<byte>>
                {
                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "System.ReadOnlySpan<byte>", "is a ref struct");
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }

    public class SourceElementMayBeByRefLike : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public SourceElementMayBeByRefLike() : base(AnalyzerHelper.SourceElementMayBeByRefLikeRule) { }

        // Read where the attribute is: on the open type the element is the type parameter, and a constraint
        // admitting a ref struct guarantees nothing about boxing.
        [Fact]
        // A constraint that admits a ref struct does not say this source fails - the substitution decides, and one
        // that is not by-ref-like reads fine at run time. The compiler cannot see which, so this warns rather than
        // refusing code that runs; a concrete ref struct stays an error.
        public async Task A_source_yielding_a_parameter_admitting_a_ref_struct_reports_warning()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public abstract class BaseClass<T> where T : allows ref struct
                {
                    public static IEnumerable<T> Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Warning, "Values", "T", "admits a ref struct");
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }

    public class SourceMustNotBeAmbiguouslyEnumerable : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
    {
        public SourceMustNotBeAmbiguouslyEnumerable() : base(AnalyzerHelper.SourceMustNotBeAmbiguouslyEnumerableRule) { }

        [Fact]
        public async Task A_source_that_is_both_shapes_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections;
                using System.Collections.Generic;
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BothShapes : IEnumerable<int>, IAsyncEnumerable<int>
                {
                    public IEnumerator<int> GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                    public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) => null;
                }

                public class BenchmarkClass
                {
                    public static BothShapes Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "BothShapes");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_source_that_is_only_an_async_enumerable_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public static IAsyncEnumerable<int> Values() => null;

                    [ParamsSource(nameof(Values))]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_source_with_several_enumerable_instantiations_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections;
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class TwoElementTypes : IEnumerable<int>, IEnumerable<string>
                {
                    IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }

                public class BenchmarkClass
                {
                    public static TwoElementTypes Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "TwoElementTypes");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_source_with_several_async_enumerable_instantiations_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Collections.Generic;
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class TwoAsyncElementTypes : IAsyncEnumerable<int>, IAsyncEnumerable<string>
                {
                    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken) => null;
                    IAsyncEnumerator<string> IAsyncEnumerable<string>.GetAsyncEnumerator(CancellationToken cancellationToken) => null;
                }

                public class BenchmarkClass
                {
                    public static TwoAsyncElementTypes Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public int MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "TwoAsyncElementTypes");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_source_whose_element_types_are_convertible_still_reports_error()
        {
            // Type inference needs a *unique* candidate interface, so it fails here too - it does not quietly
            // settle on object. This is why the rule counts instantiations instead of asking whether one element
            // type is assignable to the other.
            var testCode = /* lang=c#-test */ """
                using System.Collections;
                using System.Collections.Generic;
                using BenchmarkDotNet.Attributes;

                public class StringAndObject : IEnumerable<string>, IEnumerable<object>
                {
                    IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;
                    IEnumerator<object> IEnumerable<object>.GetEnumerator() => null;
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }

                public class BenchmarkClass
                {
                    public static StringAndObject Values() => null;

                    [ParamsSource({|#0:nameof(Values)|})]
                    public string MyParam { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Values", "StringAndObject");
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }

    public static TheoryData<string, string> UniqueParameterAttributesTheoryData
        => new()
        {
            { "Params", "Params(3)" },
            { "ParamsSource", "ParamsSource(\"test\")" },
            { "ParamsAllValues", "ParamsAllValues" }
        };

    public static TheoryData<string, int, int[]> DuplicateSameAttributeUsagesTheoryData
    {
        get
        {
            var theoryData = new TheoryData<string, int, int[]>();

            foreach (var (CurrentUniqueAttributeUsage, CurrentUniqueAttributeUsagePosition, Counts) in GenerateDuplicateSameAttributeUsageCombinations(UniqueParameterAttributesTheoryData))
            {
                theoryData.Add(CurrentUniqueAttributeUsage, CurrentUniqueAttributeUsagePosition, Counts);
            }

            return theoryData;
        }
    }

    public static TheoryData<int[]> DuplicateAttributeUsageCountsTheoryData
        => [.. GenerateDuplicateAttributeUsageCombinations(UniqueParameterAttributesTheoryData)];

    private static IEnumerable<int[]> GenerateDuplicateAttributeUsageCombinations(TheoryData<string, string> uniqueAttributeUsages)
    {
        var uniqueAttributeUsagesList = uniqueAttributeUsages.ToList().AsReadOnly();

        var allCombinations = CombinationsGenerator.GenerateCombinationsCounts(uniqueAttributeUsagesList.Count, 1);

        foreach (var currentCombination in allCombinations)
        {
            if (currentCombination.Sum() >= 2)
            {
                yield return currentCombination;
            }
        }
    }

    private static ReadOnlyCollection<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> GenerateDuplicateSameAttributeUsageCombinations(TheoryData<string, string> uniqueAttributeUsages)
    {
        var uniqueAttributeUsagesList = uniqueAttributeUsages
            .Select(tdr => (tdr[1] as string)!)
            .ToList()
            .AsReadOnly();

        var finalCombinationsList = new List<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)>();

        var allCombinations = CombinationsGenerator.GenerateCombinationsCounts(uniqueAttributeUsagesList.Count, 2)
            .ToList()
            .AsReadOnly();

        for (var i = 0; i < uniqueAttributeUsagesList.Count; i++)
        {
            foreach (var currentCombination in allCombinations)
            {
                if (currentCombination[i] > 0)
                {
                    finalCombinationsList.Add((uniqueAttributeUsagesList[i], i, currentCombination));
                }
            }
        }

        return finalCombinationsList.AsReadOnly();
    }
}