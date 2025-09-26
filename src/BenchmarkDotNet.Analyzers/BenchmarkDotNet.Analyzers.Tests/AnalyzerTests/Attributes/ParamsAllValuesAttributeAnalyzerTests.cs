namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes
{
    using Fixtures;

    using BenchmarkDotNet.Analyzers.Attributes;
    using Xunit;

    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ParamsAllValuesAttributeAnalyzerTests
    {
        public class General : AnalyzerTestFixture<ParamsAllValuesAttributeAnalyzer>
        {
            [Theory, CombinatorialData]
            public async Task A_field_or_property_not_annotated_with_the_paramsallvalues_attribute_should_not_trigger_diagnostic([CombinatorialValues("", "[Dummy]")] string missingParamsAttributeUsage,
                                                                                                                                 [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                                                                 [CombinatorialMemberData(nameof(InvalidTypes))] string invalidType)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {missingParamsAttributeUsage}
    public {invalidType} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyAttribute();
                ReferenceDummyEnumWithFlagsAttribute();

                await RunAsync();
            }

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> InvalidTypes => new TheoryData<string>
                                                              {
                                                                  "byte",
                                                                  "char",
                                                                  "double",
                                                                  "float",
                                                                  "int",
                                                                  "long",
                                                                  "sbyte",
                                                                  "short",
                                                                  "string",
                                                                  "uint",
                                                                  "ulong",
                                                                  "ushort",
                                                                                                       
                                                                  "DummyEnumWithFlagsAttribute",
                                                              
                                                                  "object",
                                                                  "System.Type"
                                                              };
        }

        public class NotAllowedOnFlagsEnumPropertyOrFieldType : AnalyzerTestFixture<ParamsAllValuesAttributeAnalyzer>
        {
            public NotAllowedOnFlagsEnumPropertyOrFieldType() : base(ParamsAllValuesAttributeAnalyzer.NotAllowedOnFlagsEnumPropertyOrFieldTypeRule) { }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_of_nonnullable_nonenum_type_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration, [CombinatorialMemberData(nameof(NonEnumTypes))] string nonEnumType)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {nonEnumType} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_of_nullable_nonenum_type_should_not_trigger_diagnostic(bool isNullable, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration, [CombinatorialMemberData(nameof(NonEnumStructs))] string nonEnumType)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {nonEnumType}{(isNullable ? "?" : "")} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_of_enum_type_without_a_flags_attribute_should_not_trigger_diagnostic(bool isNullable, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public DummyEnum{(isNullable ? "?" : "")} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyEnum();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_of_enum_type_with_a_flags_attribute_should_trigger_diagnostic(bool isNullable, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
            {
                const string enumTypeName = "DummyEnumWithFlagsAttribute";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {{|#0:{enumTypeName}|}}{(isNullable ? "?" : "")} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyEnumWithFlagsAttribute();
                AddDefaultExpectedDiagnostic(enumTypeName);

                await RunAsync();
            }

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> NonEnumStructs => new List<string> {
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
                                                                                 }.AsReadOnly();

            public static IEnumerable<string> NonEnumTypes => NonEnumStructs.Concat(new[]{ "string", "object", "System.Type" });
        }

        public class PropertyOrFieldTypeMustBeEnumOrBool : AnalyzerTestFixture<ParamsAllValuesAttributeAnalyzer>
        {
            public PropertyOrFieldTypeMustBeEnumOrBool() : base(ParamsAllValuesAttributeAnalyzer.PropertyOrFieldTypeMustBeEnumOrBoolRule) { }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_of_enum_or_bool_type_should_not_trigger_diagnostic(bool isNullable, [CombinatorialValues("DummyEnum", "bool")] string enumOrBoolType, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {enumOrBoolType}{(isNullable ? "?" : "")}  {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyEnum();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_not_of_nonnullable_enum_or_bool_type_should_trigger_diagnostic([CombinatorialMemberData(nameof(NonEnumOrBoolTypes))] string nonEnumOrBoolType, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {{|#0:{nonEnumOrBoolType}|}} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyEnum();
                AddDefaultExpectedDiagnostic();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_field_or_property_not_of_nullable_enum_or_bool_type_should_trigger_diagnostic(bool isNullable, [CombinatorialMemberData(nameof(NonEnumOrBoolStructs))] string nonEnumOrBoolType, [CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [ParamsAllValues]
    public {{|#0:{nonEnumOrBoolType}|}}{(isNullable ? "?" : "")} {fieldOrPropertyDeclaration}
}}";

                TestCode = testCode;
                ReferenceDummyEnum();
                AddDefaultExpectedDiagnostic();

                await RunAsync();
            }

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> NonEnumOrBoolStructs => new List<string> {
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
                                                                                           "ushort"
                                                                                       }.AsReadOnly();

            public static IEnumerable<string> NonEnumOrBoolTypes => NonEnumOrBoolStructs.Concat(new[] {  "string", "object", "System.Type" });
        }
    }
}
