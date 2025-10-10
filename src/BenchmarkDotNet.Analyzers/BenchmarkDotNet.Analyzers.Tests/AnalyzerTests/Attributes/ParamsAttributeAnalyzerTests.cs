namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes
{
    using Fixtures;

    using BenchmarkDotNet.Analyzers.Attributes;
    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ParamsAttributeAnalyzerTests
    {
        public class General : AnalyzerTestFixture<ParamsAttributeAnalyzer>
        {
            [Theory, CombinatorialData]
            public async Task A_field_or_property_not_annotated_with_the_params_attribute_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();
        }

        public class MustHaveValues : AnalyzerTestFixture<ParamsAttributeAnalyzer>
        {
            public MustHaveValues() : base(ParamsAttributeAnalyzer.MustHaveValuesRule) { }

            [Theory, CombinatorialData]
            public async Task Providing_one_or_more_values_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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
            public async Task Providing_an_array_with_a_rank_specifier_size_higher_than_zero_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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
            public async Task Providing_no_values_should_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> DummyAttributeUsage => DummyAttributeUsageTheoryData;

            public static IEnumerable<int> ScalarValuesListLength => Enumerable.Range(1, ScalarValues.Count);

            private static ReadOnlyCollection<string> ScalarValues => Enumerable.Range(1, 3)
                                                                                .Select(i => $"\"test{i}\"")
                                                                                .ToList()
                                                                                .AsReadOnly();
            public static IEnumerable<string> ScalarValuesContainerAttributeArgument => ScalarValuesContainerAttributeArgumentTheoryData();

            public static IEnumerable<string> EmptyParamsAttributeUsagesWithLocationMarker()
            {
                yield return "{|#0:Params|}";
                yield return "Params{|#0:()|}";
                yield return "Params({|#0:Priority = 1|})";

                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "values: "
                                      };

                var priorityNamedParameterUsages = new List<string>
                                                   {
                                                       "",
                                                       ", Priority = 1"
                                                   };

                var attributeUsagesBase = new List<string>
                                          {
                                              "Params({0}new object[] {{|#0:{{ }}|}}{1})",
                                              "Params({0}{{|#0:new object[0]|}}{1})",
                                              "Params({0}{{|#0:[ ]|}}{1})",
                                          };

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

        public class UnexpectedValueType : AnalyzerTestFixture<ParamsAttributeAnalyzer>
        {
            public UnexpectedValueType() : base(ParamsAttributeAnalyzer.UnexpectedValueTypeRule) { }

            [Theory, CombinatorialData]
            public async Task Providing_a_field_or_property_with_an_unknown_type_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                                               [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                                               [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
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
            public async Task Providing_convertible_value_types_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                              [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                              [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, "(byte)42")}})]
                                                        public int {{fieldOrPropertyDeclaration}}
                                                    }
                                                    """;
                TestCode = testCode;

                ReferenceDummyAttribute();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_both_expected_and_unexpected_value_types_should_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                                           [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                                           [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
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
            public async Task Providing_an_unknown_value_type_should_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                        [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                        [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgument))] string scalarValuesContainerAttributeArgument)
            {
                const string expectedFieldOrPropertyType = "int";
                const string valueWithUnknownType = "dummy_literal";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"42, {{|#0:{valueWithUnknownType}|}}, 33")}})]
                                                        public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                                                    }
                                                    """;
                TestCode = testCode;

                ReferenceDummyAttribute();
                DisableCompilerDiagnostics();
                AddDefaultExpectedDiagnostic(valueWithUnknownType, expectedFieldOrPropertyType, "<unknown>");

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(NotConvertibleValueTypeCombinations))]
            public async Task Providing_an_unexpected_or_not_convertible_value_type_should_trigger_diagnostic(string fieldOrPropertyDeclaration,
                                                                                                              string dummyAttributeUsage,
                                                                                                              string[] valueAndType,
                                                                                                              string scalarValuesContainerAttributeArgument)
            {
                const string expectedFieldOrPropertyType = "decimal";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [{{dummyAttributeUsage}}Params({{string.Format(scalarValuesContainerAttributeArgument, $"{{|#0:{valueAndType[0]}|}}")}})]
                                                        public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                                                    }
                                                    """;
                TestCode = testCode;
                ReferenceDummyAttribute();
                ReferenceDummyEnum();
                AddDefaultExpectedDiagnostic(valueAndType[0], expectedFieldOrPropertyType, valueAndType[1]);

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(UnexpectedArrayValueTypeCombinations))]
            public async Task Providing_an_unexpected_array_value_type_to_params_attribute_should_trigger_diagnostic(string fieldOrPropertyDeclaration,
                                                                                                                     string dummyAttributeUsage,
                                                                                                                     string[] valueAndType,
                                                                                                                     string[] arrayValuesContainerAttributeArgument)
            {
                const string expectedFieldOrPropertyType = "decimal";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [{{dummyAttributeUsage}}Params({{string.Format(arrayValuesContainerAttributeArgument[0], valueAndType[0], valueAndType[1])}})]
                                                        public {{expectedFieldOrPropertyType}} {{fieldOrPropertyDeclaration}}
                                                    }
                                                    """;
                TestCode = testCode;
                ReferenceDummyAttribute();
                ReferenceDummyEnum();
                AddDefaultExpectedDiagnostic(
                                             string.Format(arrayValuesContainerAttributeArgument[1],
                                                           valueAndType[0],
                                                           valueAndType[1]),
                                             expectedFieldOrPropertyType,
                                             $"{valueAndType[1]}[]");

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_an_empty_array_value_when_type_of_attribute_target_is_not_object_array_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                                                                             [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                                                                             [CombinatorialMemberData(nameof(EmptyValuesAttributeArgument))] string emptyValuesAttributeArgument)
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
            public async Task Providing_an_empty_array_value_when_type_of_attribute_target_is_object_array_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
                                                                                                                                         [CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                                                                         [CombinatorialMemberData(nameof(EmptyValuesAttributeArgument))] string emptyValuesAttributeArgument)
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

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> DummyAttributeUsage => DummyAttributeUsageTheoryData;

            public static IEnumerable<string> EmptyValuesAttributeArgument()
            {
                yield return "";
                yield return "()";
                yield return "(Priority = 1)";

                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "values: "
                                      };

                var priorityNamedParameterUsages = new List<string>
                                                   {
                                                       "",
                                                       ", Priority = 1"
                                                   };

                var attributeUsagesBase = new List<string>
                                          {
                                              "({0}new object[] {{ }}{1})",
                                              "({0}new object[0]{1})",
                                              "({0}[ ]{1})"
                                          };

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

            public static IEnumerable<object[]> UnexpectedArrayValueTypeCombinations => CombinationsGenerator.CombineArguments(FieldOrPropertyDeclarations, DummyAttributeUsage, ValuesAndTypes, ArrayValuesContainerAttributeArgumentWithLocationMarker());

            public static IEnumerable<object[]> NotConvertibleValueTypeCombinations => CombinationsGenerator.CombineArguments(FieldOrPropertyDeclarations, DummyAttributeUsage, NotConvertibleValuesAndTypes, ScalarValuesContainerAttributeArgument);

            public static IEnumerable<string> ScalarValuesContainerAttributeArgument => ScalarValuesContainerAttributeArgumentTheoryData();

            public static IEnumerable<string[]> ArrayValuesContainerAttributeArgumentWithLocationMarker()
            {
                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "values: "
                                      };

                var priorityNamedParameterUsages = new List<string>
                                                   {
                                                       "",
                                                       ", Priority = 1"
                                                   };

                var attributeUsagesBase = new List<(string, string)>
                                          {
                                              ("{0}new object[] {{{{ {{{{|#0:new[] {{{{ {{0}} }}}}|}}}} }}}}{1}",       "new[] {{ {0} }}"),       // new object[] { new[] { 42 } }
                                              ("{0}new object[] {{{{ {{{{|#0:new {{1}}[] {{{{ {{0}} }}}}|}}}} }}}}{1}", "new {1}[] {{ {0} }}"),   // new object[] { new int[] { 42 } }
                                              ("{0}[ {{{{|#0:new[] {{{{ {{0}} }}}}|}}}} ]{1}",                          "new[] {{ {0} }}"),       // [ new[] { 42 } ]
                                              ("{0}[ {{{{|#0:new {{1}}[] {{{{ {{0}} }}}}|}}}} ]{1}",                    "new {1}[] {{ {0} }}"),   // [ new int[] { 42 } ]
                                              ("{0}new object[] {{{{ {{{{|#0:new {{1}}[] {{{{ }}}}|}}}} }}}}{1}",       "new {1}[] {{ }}"),       // new object[] { new int[] { } }
                                              ("{0}[ {{{{|#0:new {{1}}[] {{{{ }}}}|}}}} ]{1}",                          "new {1}[] {{ }}"),       // [ new int[] { } ]
                                              ("{0}new object[] {{{{ {{{{|#0:new {{1}}[0]|}}}} }}}}{1}",                "new {1}[0]"),            // new object[] { new int[0] }
                                              ("{0}[ {{{{|#0:new {{1}}[0]|}}}} ]{1}",                                   "new {1}[0]"),            // [ new int[0] ]
                                          };

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                        {
                            yield return [
                                string.Format(attributeUsageBase.Item1, nameColonUsage, priorityNamedParameterUsage),
                                attributeUsageBase.Item2
                            ];
                        }
                    }
                }
            }

            public static IEnumerable<string[]> ValuesAndTypes =>
            [
                [ "true", "bool" ],
                [ "(byte)123", "byte" ],
                [ "'A'", "char" ],
                [ "1.0D", "double" ],
                [ "1.0F", "float" ],
                [ "123", "int" ],
                [ "123L", "long" ],
                [ "(sbyte)-100", "sbyte" ],
                [ "(short)-123", "short" ],
                [ """
                  "test"
                  """, "string" ],
                [ "123U", "uint" ],
                [ "123UL", "ulong" ],
                [ "(ushort)123", "ushort" ],

                [ """
                  (object)"test_object"
                  """, "object" ],
                [ "typeof(string)", "System.Type" ],
                [ "DummyEnum.Value1", "DummyEnum" ]
            ];

            public static IEnumerable<string[]> NotConvertibleValuesAndTypes =>
            [
                [ "true", "bool" ],
                [ "1.0D", "double" ],
                [ "1.0F", "float" ],
                [ """
                  "test"
                  """, "string" ],

                [ """
                  (object)"test_object"
                  """, "object" ],
                [ "typeof(string)", "System.Type" ],
                [ "DummyEnum.Value1", "DummyEnum" ]
            ];
        }

        public class UnnecessarySingleValuePassedToAttribute : AnalyzerTestFixture<ParamsAttributeAnalyzer>
        {
            public UnnecessarySingleValuePassedToAttribute() : base(ParamsAttributeAnalyzer.UnnecessarySingleValuePassedToAttributeRule) { }

            [Theory, CombinatorialData]
            public async Task Providing_two_or_more_values_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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
            public async Task Providing_only_a_single_value_should_trigger_diagnostic([CombinatorialMemberData(nameof(FieldOrPropertyDeclarations))] string fieldOrPropertyDeclaration,
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
                AddDefaultExpectedDiagnostic();
                ReferenceDummyAttribute();

                await RunAsync();
            }

            public static IEnumerable<string> FieldOrPropertyDeclarations => new FieldOrPropertyDeclarationTheoryData();

            public static IEnumerable<string> DummyAttributeUsage => DummyAttributeUsageTheoryData;

            public static IEnumerable<int> ScalarValuesListLength => Enumerable.Range(2, ScalarValues.Count);

            private static ReadOnlyCollection<string> ScalarValues => Enumerable.Range(1, 2)
                                                                                .Select(i => $"\"test{i}\"")
                                                                                .ToList()
                                                                                .AsReadOnly();
            public static IEnumerable<string> ScalarValuesContainerAttributeArgument => ScalarValuesContainerAttributeArgumentTheoryData();

            public static IEnumerable<string> ScalarValuesContainerAttributeArgumentWithLocationMarker()
            {
                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "values: "
                                      };

                var priorityNamedParameterUsages = new List<string>
                                                   {
                                                       "",
                                                       ", Priority = 1"
                                                   };

                var attributeUsagesBase = new List<string>
                                          {
                                              "{{{{|#0:{{0}}|}}}}{1}",
                                              "{0}new object[] {{{{ {{{{|#0:{{0}}|}}}} }}}}{1}",
                                              "{0}[ {{{{|#0:{{0}}|}}}} ]{1}",
                                          };

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

        public static TheoryData<string> DummyAttributeUsageTheoryData => [
                                                                              "",
                                                                              "Dummy, "
                                                                          ];

        public static TheoryData<string> ScalarValuesContainerAttributeArgumentTheoryData()
        {
            return new TheoryData<string>(GenerateData());

            static IEnumerable<string> GenerateData()
            {
                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "values: "
                                      };

                var priorityNamedParameterUsages = new List<string>
                                                   {
                                                       "",
                                                       ", Priority = 1"
                                                   };

                var attributeUsagesBase = new List<string>
                                          {
                                              "{{0}}{1}",
                                              "{0}new object[] {{{{ {{0}} }}}}{1}",
                                              "{0}[ {{0}} ]{1}"
                                          };

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
}
