namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.Attributes
{
    using Fixtures;

    using Analyzers.Attributes;

    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class ArgumentsAttributeAnalyzerTests
    {
        public class General : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
        {
            [Theory, CombinatorialData]
            public async Task A_method_annotated_with_an_arguments_attribute_with_no_values_and_the_benchmark_attribute_and_having_no_parameters_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(EmptyArgumentsAttributeUsages))] string emptyArgumentsAttributeUsage,
                                                                                                                                                                               [CombinatorialRange(1, 2)] int attributeUsageMultiplier)
            {
                var emptyArgumentsAttributeUsages = new List<string>();

                for (var i = 0; i < attributeUsageMultiplier; i++)
                {
                    emptyArgumentsAttributeUsages.Add(emptyArgumentsAttributeUsage);
                }

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        {{string.Join("\n", emptyArgumentsAttributeUsages)}}
                                                        public void BenchmarkMethod()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                await RunAsync();
            }

            public static IEnumerable<string> EmptyArgumentsAttributeUsages()
            {
                yield return "[Arguments]";
                yield return "[Arguments()]";
                yield return "[Arguments(Priority = 1)]";

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
                                              "[Arguments({0}new object[] {{ }}{1})]",
                                              "[Arguments({0}new object[0]{1})]",
                                              "[Arguments({0}[]{1})]",
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

            public static TheoryData<int> ArgumentAttributeUsagesListLength => new(Enumerable.Range(1, ArgumentAttributeUsages.Count));

            private static ReadOnlyCollection<string> ArgumentAttributeUsages => new List<string> {
                                                                                     "Arguments",
                                                                                     "Arguments()",
                                                                                     "Arguments(42, \"test\")"
                                                                                 }.AsReadOnly();
        }

        public class MethodWithoutAttributeMustHaveNoParameters : AnalyzerTestFixture<ArgumentsAttributeAnalyzer>
        {
            public MethodWithoutAttributeMustHaveNoParameters() : base(ArgumentsAttributeAnalyzer.MethodWithoutAttributeMustHaveNoParametersRule) { }

            [Fact]
            public async Task A_method_annotated_with_an_arguments_attribute_and_the_benchmark_attribute_and_having_parameters_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark]
                                                               [Arguments(42, "test")]
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
            public async Task A_method_with_parameters_and_no_arguments_or_benchmark_attributes_should_not_trigger_diagnostic(int parametersListLength)
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
            public async Task A_method_annotated_with_the_benchmark_attribute_but_no_arguments_attribute_with_parameters_should_trigger_diagnostic(int parametersListLength)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        public void BenchmarkMethod({|#0:{{string.Join(", ", Parameters.Take(parametersListLength))}}|})
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic();

                await RunAsync();
            }

            public static TheoryData<int> ParametersListLength => new(Enumerable.Range(1, Parameters.Count));

            private static ReadOnlyCollection<string> Parameters => new List<string> {
                                                                        "int a",
                                                                        "string b",
                                                                        "bool c"
                                                                    }.AsReadOnly();
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
            public async Task Having_a_mismatching_value_count_should_trigger_diagnostic([CombinatorialMemberData(nameof(ArgumentsAttributeUsagesWithLocationMarker))] string argumentsAttributeUsage,
                                                                                         [CombinatorialMemberData(nameof(ParameterLists))] (string Parameters, int ParameterCount, string PluralSuffix) parameterData)
            {
                const string benchmarkMethodName = "BenchmarkMethod";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        {{argumentsAttributeUsage}}
                                                        public void {{benchmarkMethodName}}({{parameterData.Parameters}})
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;
                TestCode = testCode;
                AddExpectedDiagnostic(0, parameterData.ParameterCount, parameterData.PluralSuffix, benchmarkMethodName, 2);
                AddExpectedDiagnostic(1, parameterData.ParameterCount, parameterData.PluralSuffix, benchmarkMethodName, 3);

                await RunAsync();
            }

            public static IEnumerable<(string, int, string)> ParameterLists => [ ("string a", 1, ""), ("", 0, "s") ];

            public static TheoryData<string> ArgumentsAttributeUsages()
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
                                                  "[Arguments({1}{2})]",
                                                  "[Arguments({0}new object[] {{ {1} }}{2})]",
                                                  "[Arguments({0}[ {1} ]{2})]"
                                              };

                    var valueLists = new List<string>
                                     {
                                         "42, \"test\"",
                                         "\"value\", 100"
                                     };

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
                return new TheoryData<string>(GenerateData());

                static IEnumerable<string> GenerateData()
                {
                    yield return "[{|#0:Arguments|}]";
                    yield return "[Arguments{|#0:()|}]";
                    yield return "[Arguments({|#0:Priority = 1|})]";

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
                                                  "[Arguments({0}new object[] {{|#0:{{ }}|}}{1})]",
                                                  "[Arguments({0}new object[{{|#0:0|}}]{1})]",
                                                  "[Arguments({0}{{|#0:[]|}}{1})]",
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

            public static IEnumerable<string> ArgumentsAttributeUsagesWithLocationMarker()
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
                                              "[Arguments({{|#{1}:{2}|}}{3})]",
                                              "[Arguments({0}new object[] {{ {{|#{1}:{2}|}} }}{3})]",
                                              "[Arguments({0}[ {{|#{1}:{2}|}} ]{3})]"
                                          };

                var valueLists = new List<string>
                                 {
                                     "42, \"test\"",
                                     "\"value\", 100, false"
                                 };

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        foreach (var priorityNamedParameterUsage in priorityNamedParameterUsages)
                        {
                            yield return string.Join("\n    ", valueLists.Select((vv, i) => string.Format(attributeUsageBase, nameColonUsage, i, vv, priorityNamedParameterUsage)));
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
            public async Task Having_a_mismatching_value_count_with_nonempty_argument_attribute_usages_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument,
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
            public async Task Providing_expected_value_type_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                          [CombinatorialMemberData(nameof(ValuesAndTypes))] ValueTupleDouble<string, string> valueAndType,
                                                                                          [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
            {
                var testCode = /* lang=c#-test */ $$"""
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

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_integer_value_types_within_target_type_range_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
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
            public async Task Providing_implicitly_convertible_value_type_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
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
            public async Task Having_unknown_parameter_type_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
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
            public async Task Providing_an_unexpected_or_not_implicitly_convertible_value_type_should_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
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
            public async Task Providing_integer_value_types_not_within_target_type_range_should_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
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
            public async Task Providing_an_unkown_value_type_should_trigger_diagnostic([CombinatorialMemberData(nameof(DummyAttributeUsage))] string dummyAttributeUsage,
                                                                                       [CombinatorialMemberData(nameof(ScalarValuesContainerAttributeArgumentEnumerableLocal))] string scalarValuesContainerAttributeArgument)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        [{{dummyAttributeUsage}}{{string.Format(scalarValuesContainerAttributeArgument, "{|#0:dummy_literal|}, true")}}]
                                                        public void BenchmarkMethod(byte a, bool b)
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                ReferenceDummyAttribute();

                DisableCompilerDiagnostics();
                AddDefaultExpectedDiagnostic("dummy_literal", "byte", "<unknown>");

                await RunAsync();
            }

            public static IEnumerable<string> DummyAttributeUsage => DummyAttributeUsageTheoryData;

            public static TheoryData<string> EmptyArgumentsAttributeUsagesWithMismatchingValueCount()
            {
                return new TheoryData<string>(GenerateData());

                static IEnumerable<string> GenerateData()
                {
                    yield return "[Arguments]";
                    yield return "[Arguments()]";
                    yield return "[Arguments(Priority = 1)]";

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
                                                  "[Arguments({0}new object[] {{ }}{1})]",
                                                  "[Arguments({0}new object[0]{1})]",
                                                  "[Arguments({0}[]{1})]",
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

            public static IEnumerable<string> ScalarValuesContainerAttributeArgumentEnumerableLocal => ScalarValuesContainerAttributeArgumentEnumerable();

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
                ( "DummyEnum.Value1", "DummyEnum" )
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
        }

        public static TheoryData<string> DummyAttributeUsageTheoryData => [
                                                                              "",
                                                                              "Dummy, "
                                                                          ];

        private static IEnumerable<string> ScalarValuesContainerAttributeArgumentEnumerable()
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
                                          "Arguments({{0}}{1})",
                                          "Arguments({0}new object[] {{{{ {{0}} }}}}{1})",
                                          "Arguments({0}[ {{0}} ]{1})"
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
