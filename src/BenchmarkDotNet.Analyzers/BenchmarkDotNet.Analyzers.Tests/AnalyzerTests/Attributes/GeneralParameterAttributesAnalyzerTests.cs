﻿namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes
{
    using Fixtures;

    using Analyzers.Attributes;

    using Xunit;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class GeneralParameterAttributesAnalyzerTests
    {
        public class MutuallyExclusiveOnField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public MutuallyExclusiveOnField() : base(GeneralParameterAttributesAnalyzer.MutuallyExclusiveOnFieldRule) { }

            [Fact]
            public async Task A_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    public int _field = 0, _field2 = 1;
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task A_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    public int _field = 0, _field2 = 1;
}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Fact]
            public async Task A_field_annotated_with_a_duplicate_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode
= /* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    [Dummy]
    public int _field = 0, _field2 = 1;
}";

                TestCode = testCode;
                ReferenceDummyAttribute();
                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_field_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int _field = 0, _field2 = 1;
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int _field = 0, _field2 = 1;
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int {fieldIdentifier} = 0, field2 = 1;
}}";

                TestCode = testCode;

                for (var i = 0; i < diagnosticCounter; i++)
                {
                    AddExpectedDiagnostic(i, fieldIdentifier);
                }

                await RunAsync();
            }

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

            public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData;

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }

        public class MutuallyExclusiveOnProperty : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public MutuallyExclusiveOnProperty() : base(GeneralParameterAttributesAnalyzer.MutuallyExclusiveOnPropertyRule) { }

            [Fact]
            public async Task A_property_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    public int Property { get; set; }
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task A_property_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    public int Property { get; set; }
}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Fact]
            public async Task A_property_annotated_with_a_duplicate_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    [Dummy]
    public int Property { get; set; }
}";

                TestCode = testCode;
                ReferenceDummyAttribute();
                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_property_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
            public async Task A_property_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(string currentAttributeUsage, int currentUniqueAttributeUsagePosition, int[] duplicateSameAttributeUsageCounts)
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int Property {{ get; set; }}
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int {propertyIdentifier} {{ get; set; }}
}}";

                TestCode = testCode;

                for (var i = 0; i < diagnosticCounter; i++)
                {
                    AddExpectedDiagnostic(i, propertyIdentifier);
                }

                await RunAsync();
            }

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

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
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    {classMemberAccessModifier}int _field = 0, _field2 = 1;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
            public async Task A_nonpublic_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
            {
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    [Dummy]
    {classMemberAccessModifier}int _field = 0, _field2 = 1;
}}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_public_field_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int _field = 0, _field2 = 2;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_nonpublic_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    {classMemberAccessModifier}int _field = 0, _field2 = 1;
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    {classMemberAccessModifier}int _field = 0, _field2 = 1;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_nonpublic_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic([CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
                                                                                                                      [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
            {
                const string fieldIdentifier = "_field";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attribute.AttributeUsage}]
    {classMemberAccessModifier}int {{|#0:{fieldIdentifier}|}} = 0, field2 = 0;
}}";
                TestCode = testCode;
                AddDefaultExpectedDiagnostic(fieldIdentifier, attribute.AttributeName);

                await RunAsync();
            }

            public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicClassMemberAccessModifiers);

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

#if NET6_0_OR_GREATER
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));
#else
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => (tdr[0] as string, tdr[1] as string));
#endif
            public static IEnumerable<string> NonPublicClassMemberAccessModifiers => new NonPublicClassMemberAccessModifiersTheoryData();

#if NET6_0_OR_GREATER
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));
#else
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => (tdr[0] as string, (int)tdr[1], tdr[2] as int[]));
#endif
            public static IEnumerable<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }

        public class PropertyMustBePublic : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public PropertyMustBePublic() : base(GeneralParameterAttributesAnalyzer.PropertyMustBePublic) { }

            [Theory]
            [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
            public async Task A_nonpublic_property_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
            {
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    {classMemberAccessModifier}int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
            public async Task A_nonpublic_property_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string classMemberAccessModifier)
            {
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    [Dummy]
    {classMemberAccessModifier}int Property {{ get; set; }}
}}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_public_property_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_nonpublic_property_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    {classMemberAccessModifier}int Property {{ get; set; }}
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    {classMemberAccessModifier}int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_nonpublic_property_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic([CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
                                                                                                                         [CombinatorialMemberData(nameof(NonPublicClassMemberAccessModifiers))] string classMemberAccessModifier)
            {
                const string propertyIdentifier = "Property";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attribute.AttributeUsage}]
    {classMemberAccessModifier}int {{|#0:{propertyIdentifier}|}} {{ get; set; }}
}}";
                TestCode = testCode;
                AddDefaultExpectedDiagnostic(propertyIdentifier, attribute.AttributeName);

                await RunAsync();
            }

            public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicClassMemberAccessModifiersCombinations => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicClassMemberAccessModifiers);

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

#if NET6_0_OR_GREATER
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));
#else
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => (tdr[0] as string, tdr[1] as string));
#endif

            public static IEnumerable<string> NonPublicClassMemberAccessModifiers => new NonPublicClassMemberAccessModifiersTheoryData();

#if NET6_0_OR_GREATER
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));
#else
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => (tdr[0] as string, (int)tdr[1], tdr[2] as int[]));
#endif

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }

        public class NotValidOnReadonlyField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public NotValidOnReadonlyField() : base(GeneralParameterAttributesAnalyzer.NotValidOnReadonlyFieldRule) { }

            [Fact]
            public async Task A_readonly_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    public readonly int _field = 0, _field2 = 1;
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task A_readonly_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    public readonly int _field = 0, _field2 = 1;
}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_field_without_a_readonly_modifier_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int _field = 0, _field2 = 1;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
            public async Task A_readonly_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(string currentAttributeUsage, int currentUniqueAttributeUsagePosition, int[] duplicateSameAttributeUsageCounts)
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public readonly int _field = 0, _field2 = 1;
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public readonly int _field = 0, _field2 = 1;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributes))]
            public async Task A_readonly_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(string attributeName, string attributeUsage)
            {
                const string fieldIdentifier = "_field";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public {{|#0:readonly|}} int {fieldIdentifier} = 0, field2 = 1;
}}";
                TestCode = testCode;
                AddDefaultExpectedDiagnostic(fieldIdentifier, attributeName);

                await RunAsync();
            }

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

            public static TheoryData<string, string> UniqueParameterAttributes => UniqueParameterAttributesTheoryData;

            public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData;

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }

        public class NotValidOnConstantField : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public NotValidOnConstantField() : base(GeneralParameterAttributesAnalyzer.NotValidOnConstantFieldRule) { }

            [Fact]
            public async Task A_constant_field_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    public const int Constant = 0;
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task A_constant_field_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    public const int Constant = 0;
}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
            public async Task A_constant_field_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(string currentAttributeUsage, int currentUniqueAttributeUsagePosition, int[] duplicateSameAttributeUsageCounts)
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public const int Constant = 0;
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public const int Constant = 0;
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributes))]
            public async Task A_constant_field_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(string attributeName, string attributeUsage)
            {
                const string constantIdentifier = "Constant";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public {{|#0:const|}} int {constantIdentifier} = 0;
}}";
                TestCode = testCode;
                AddDefaultExpectedDiagnostic(constantIdentifier, attributeName);

                await RunAsync();
            }

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

            public static TheoryData<string, string> UniqueParameterAttributes => UniqueParameterAttributesTheoryData;

            public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData;

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }
#if NET5_0_OR_GREATER
        public class PropertyCannotBeInitOnly : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public PropertyCannotBeInitOnly() : base(GeneralParameterAttributesAnalyzer.PropertyCannotBeInitOnlyRule) { }

            [Fact]
            public async Task An_initonly_property_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    public int Property { get; init; }
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task An_initonly_property_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic()
            {
                const string testCode =
/* lang=c#-test */ @"public class BenchmarkClass
{
    [Dummy]
    public int Property { get; init; }
}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_property_with_an_assignable_setter_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(DuplicateSameParameterAttributeUsages))]
            public async Task An_initonly_property_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic(string currentAttributeUsage, int currentUniqueAttributeUsagePosition, int[] duplicateSameAttributeUsageCounts)
            {
                var duplicateAttributeUsages = new List<string>(1 + duplicateSameAttributeUsageCounts.Sum());

                var uniqueParameterAttributeUsages = UniqueParameterAttributeUsages.AsReadOnly();

                for (var i = 0; i < duplicateSameAttributeUsageCounts.Length; i++)
                {
                    if (i == currentUniqueAttributeUsagePosition)
                    {
                        duplicateAttributeUsages.Add($"[{currentAttributeUsage}]");
                    }

                    for (var j = 0; j <duplicateSameAttributeUsageCounts[i]; j++)
                    {
                        duplicateAttributeUsages.Add($"[{uniqueParameterAttributeUsages[i]}]");
                    }
                }

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int Property {{ get; init; }}
}}";

                TestCode = testCode;
                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(DuplicateParameterAttributeUsageCounts))]
            public async Task An_initonly_property_annotated_with_more_than_one_parameter_attribute_should_not_trigger_diagnostic(int[] duplicateAttributeUsageCounts)
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int Property {{ get; init; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributes))]
            public async Task An_initonly_property_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic(string attributeName, string attributeUsage)
            {
                const string propertyIdentifier = "Property";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int {propertyIdentifier} {{ get; {{|#0:init|}}; }}
}}";

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(propertyIdentifier, attributeName);

                await RunAsync();
            }

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

            public static TheoryData<string, string> UniqueParameterAttributes => UniqueParameterAttributesTheoryData;

            public static TheoryData<string, int, int[]> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData;

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;
        }
#endif
        public class PropertyMustHavePublicSetter : AnalyzerTestFixture<GeneralParameterAttributesAnalyzer>
        {
            public PropertyMustHavePublicSetter() : base(GeneralParameterAttributesAnalyzer.PropertyMustHavePublicSetterRule) { }

            [Theory]
            [MemberData(nameof(NonPublicPropertySettersTheoryData))]
            public async Task A_property_with_a_nonpublic_setter_not_annotated_with_any_parameter_attribute_should_not_trigger_diagnostic(string nonPublicPropertySetter)
            {
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    public int Property {nonPublicPropertySetter}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(NonPublicPropertySettersTheoryData))]
            public async Task A_property_with_a_nonpublic_setter_annotated_with_a_nonparameter_attribute_should_not_trigger_diagnostic(string nonPublicPropertySetter)
            {
                var testCode =
/* lang=c#-test */ $@"public class BenchmarkClass
{{
    [Dummy]
    public int Property {nonPublicPropertySetter}
}}";

                TestCode = testCode;
                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UniqueParameterAttributeUsages))]
            public async Task A_property_with_an_assignable_setter_annotated_with_a_unique_parameter_attribute_should_not_trigger_diagnostic(string attributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attributeUsage}]
    public int Property {{ get; set; }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_property_with_a_nonpublic_setter_annotated_with_the_same_duplicate_parameter_attribute_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DuplicateSameParameterAttributeUsages))] (string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts) duplicateSameParameterAttributeUsages,
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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int Property {nonPublicPropertySetter}
}}";

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

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join($"{Environment.NewLine}    ", duplicateAttributeUsages)}
    public int Property {nonPublicPropertySetter}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_property_with_a_nonpublic_setter_annotated_with_a_unique_parameter_attribute_should_trigger_diagnostic([CombinatorialMemberData(nameof(UniqueParameterAttributes))] (string AttributeName, string AttributeUsage) attribute,
                                                                                                                                       [CombinatorialMemberData(nameof(NonPublicPropertySetters))] string nonPublicPropertySetter)
            {
                const string propertyIdentifier = "Property";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [{attribute.AttributeUsage}]
    public int {{|#0:{propertyIdentifier}|}} {nonPublicPropertySetter}
}}";

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(propertyIdentifier, attribute.AttributeName);

                await RunAsync();
            }

            public static IEnumerable<object[]> DuplicateAttributeUsageCountsAndNonPublicPropertySetterCombinations => CombinationsGenerator.CombineArguments(DuplicateParameterAttributeUsageCounts, NonPublicPropertySetters());

#if NET6_0_OR_GREATER
            public static TheoryData<string> UniqueParameterAttributeUsages => new(UniqueParameterAttributesTheoryData.Select(tdr => (tdr[1] as string)!));
#else
            public static TheoryData<string> UniqueParameterAttributeUsages => new TheoryData<string>(UniqueParameterAttributesTheoryData.Select(tdr => tdr[1] as string));
#endif

#if NET6_0_OR_GREATER
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => ((tdr[0] as string)!, (tdr[1] as string)!));
#else
            public static IEnumerable<(string AttributeName, string AttributeUsage)> UniqueParameterAttributes => UniqueParameterAttributesTheoryData.Select(tdr => (tdr[0] as string, tdr[1] as string));
#endif

#if NET6_0_OR_GREATER
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => ((tdr[0] as string)!, (int)tdr[1], (tdr[2] as int[])!));
#else
            public static IEnumerable<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)> DuplicateSameParameterAttributeUsages => DuplicateSameAttributeUsagesTheoryData.Select(tdr => (tdr[0] as string, (int)tdr[1], tdr[2] as int[]));
#endif

            public static TheoryData<int[]> DuplicateParameterAttributeUsageCounts => DuplicateAttributeUsageCountsTheoryData;

            public static IEnumerable<string> NonPublicPropertySetters() => new NonPublicPropertySetterAccessModifiersTheoryData().Select<string, string>(m => $"{{ get; {m} set; }}")
                                                                                                                                  .Concat(new[] {
                                                                                                                                                  "{ get; }",
                                                                                                                                                  "=> 0;"
                                                                                                                                                });
            public static TheoryData<string> NonPublicPropertySettersTheoryData() => new TheoryData<string>(NonPublicPropertySetters());
        }

        public static TheoryData<string, string> UniqueParameterAttributesTheoryData => new TheoryData<string, string>
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

                foreach (var duplicateSameAttributeUsageCombination in GenerateDuplicateSameAttributeUsageCombinations(UniqueParameterAttributesTheoryData))
                {
                    theoryData.Add(duplicateSameAttributeUsageCombination.CurrentUniqueAttributeUsage, duplicateSameAttributeUsageCombination.CurrentUniqueAttributeUsagePosition, duplicateSameAttributeUsageCombination.Counts);
                }

                return theoryData;
            }
        }

        public static TheoryData<int[]> DuplicateAttributeUsageCountsTheoryData => new TheoryData<int[]>(GenerateDuplicateAttributeUsageCombinations(UniqueParameterAttributesTheoryData));

        private static IEnumerable<int[]> GenerateDuplicateAttributeUsageCombinations(TheoryData<string, string> uniqueAttributeUsages)
        {
            var uniqueAttributeUsagesList = uniqueAttributeUsages.ToList()
                                                                 .AsReadOnly();

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
#if NET6_0_OR_GREATER
            var uniqueAttributeUsagesList = uniqueAttributeUsages.Select(tdr => (tdr[1] as string)!)
                                                                 .ToList()
                                                                 .AsReadOnly();
#else
            var uniqueAttributeUsagesList = uniqueAttributeUsages.Select(tdr => tdr[1] as string)
                                                                 .ToList()
                                                                 .AsReadOnly();
#endif

            var finalCombinationsList = new List<(string CurrentUniqueAttributeUsage, int CurrentUniqueAttributeUsagePosition, int[] Counts)>();

            var allCombinations = CombinationsGenerator.GenerateCombinationsCounts(uniqueAttributeUsagesList.Count, 2).ToList()
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
}
