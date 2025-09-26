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

                var testCode = /* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {string.Join("\n", emptyArgumentsAttributeUsages)}
    public void BenchmarkMethod()
    {{
    
    }}
}}";

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
#if NET8_0_OR_GREATER
                                              "[Arguments({0}[]{1})]",
#endif
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
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    [{string.Join("]\n[", ArgumentAttributeUsages.Take(argumentAttributeUsagesListLength))}]
    public void BenchmarkMethod()
    {{
    
    }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentAttributeUsagesListLength))]
            public async Task A_method_with_at_least_one_arguments_attribute_but_no_benchmark_attribute_should_trigger_diagnostic(int argumentAttributeUsagesListLength)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    {string.Join("\n", ArgumentAttributeUsages.Take(argumentAttributeUsagesListLength).Select((a, i) => $"[{{|#{i}:{a}|}}]"))}
    public void BenchmarkMethod()
    {{
    
    }}
}}";

                TestCode = testCode;

                for (var i = 0; i < argumentAttributeUsagesListLength; i++)
                {
                    AddExpectedDiagnostic(i);
                }

                await RunAsync();
            }

            public static TheoryData<int> ArgumentAttributeUsagesListLength => new TheoryData<int>(Enumerable.Range(1, ArgumentAttributeUsages.Count));

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
                const string testCode =
/* lang=c#-test */ @"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{
    [Benchmark]
    [Arguments(42, ""test"")]
    public void BenchmarkMethod(int a, string b)
    {
    
    }
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ParametersListLength))]
            public async Task A_method_with_parameters_and_no_arguments_or_benchmark_attributes_should_not_trigger_diagnostic(int parametersListLength)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    public void BenchmarkMethod({string.Join(", ", Parameters.Take(parametersListLength))})
    {{
    
    }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ParametersListLength))]
            public async Task A_method_annotated_with_the_benchmark_attribute_but_no_arguments_attribute_with_parameters_should_trigger_diagnostic(int parametersListLength)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    public void BenchmarkMethod({{|#0:{string.Join(", ", Parameters.Take(parametersListLength))}|}})
    {{
    
    }}
}}";

                TestCode = testCode;
                AddDefaultExpectedDiagnostic();

                await RunAsync();
            }

            public static TheoryData<int> ParametersListLength => new TheoryData<int>(Enumerable.Range(1, Parameters.Count));

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
                const string testCode =
/* lang=c#-test */ @"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{
    [Benchmark]
    public void BenchmarkMethod()
    {
    
    }
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsages))]
            public async Task Having_a_matching_value_count_should_not_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(string a, bool b)
    {{

    }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(EmptyArgumentsAttributeUsagesWithLocationMarker))]
            public async Task Having_a_mismatching_empty_value_count_targeting_a_method_with_parameters_should_trigger_diagnostic(string argumentsAttributeUsage)
            {
                const string benchmarkMethodName = "BenchmarkMethod";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void {benchmarkMethodName}(string a)
    {{
    
    }}
}}";
                TestCode = testCode;
                AddDefaultExpectedDiagnostic(1, "", benchmarkMethodName, 0);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Having_a_mismatching_value_count_should_trigger_diagnostic([CombinatorialMemberData(nameof(ArgumentsAttributeUsagesWithLocationMarker))] string argumentsAttributeUsage,
                                                                                         [CombinatorialMemberData(nameof(ParameterLists))] (string Parameters, int ParameterCount, string PluralSuffix) parameterData)
            {
                const string benchmarkMethodName = "BenchmarkMethod";

                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void {benchmarkMethodName}({parameterData.Parameters})
    {{
    
    }}
}}";
                TestCode = testCode;
                AddExpectedDiagnostic(0, parameterData.ParameterCount, parameterData.PluralSuffix, benchmarkMethodName, 2);
                AddExpectedDiagnostic(1, parameterData.ParameterCount, parameterData.PluralSuffix, benchmarkMethodName, 3);

                await RunAsync();
            }

            public static IEnumerable<(string, int, string)> ParameterLists => new [] { ("string a", 1, ""), ("", 0, "s") };

            public static TheoryData<string> ArgumentsAttributeUsages()
            {
                return new TheoryData<string>(GenerateData());

#if NET6_0_OR_GREATER
                static IEnumerable<string> GenerateData()
#else
                IEnumerable<string> GenerateData()
#endif
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
#if NET8_0_OR_GREATER
                                                  "[Arguments({0}[ {1} ]{2})]"
#endif
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

#if NET6_0_OR_GREATER
                static IEnumerable<string> GenerateData()
#else
                IEnumerable<string> GenerateData()
#endif
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
#if NET8_0_OR_GREATER
                                                  "[Arguments({0}{{|#0:[]|}}{1})]",
#endif
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
#if NET8_0_OR_GREATER
                                              "[Arguments({0}[ {{|#{1}:{2}|}} ]{3})]"
#endif
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
                const string testCode =
/* lang=c#-test */ @"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{
    [Benchmark]
    public void BenchmarkMethod()
    {
    
    }
}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(EmptyArgumentsAttributeUsagesWithMismatchingValueCount))]
            public async Task Having_a_mismatching_value_count_with_empty_argument_attribute_usages_should_not_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;
public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(string a)
    {{
    
    }}
}}";
                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Having_a_mismatching_value_count_with_nonempty_argument_attribute_usages_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ArgumentsAttributeUsagesWithMismatchingValueCount))] string argumentsAttributeUsage,
                                                                                                                                     [CombinatorialValues("string a", "")] string parameters)
            {
                var testCode = /* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;
public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod({parameters})
    {{
    
    }}
}}";
                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsagesWithMatchingValueTypes))]
            public async Task Having_matching_value_types_should_not_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(int a, string b)
    {{
    
    }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsagesWithConvertibleValueTypes))]
            public async Task Providing_convertible_value_types_should_not_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(int a, string b)
    {{
    
    }}
}}";

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsagesWithMatchingValueTypes))]
            public async Task Having_unknown_parameter_type_should_not_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(unkown a, string b)
    {{
    
    }}
}}";

                TestCode = testCode;

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsagesWithMismatchingValueTypesWithLocationMarker))]
            public async Task Having_mismatching_or_not_convertible_value_types_should_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(byte a, bool b)
    {{
    
    }}
}}";

                TestCode = testCode;

                AddExpectedDiagnostic(0, "typeof(string)", "byte", "System.Type");
                AddExpectedDiagnostic(1, "\"test\"", "bool", "string");
                AddExpectedDiagnostic(2, "999", "byte", "int");

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(ArgumentsAttributeUsagesWithUnknownValueTypesWithLocationMarker))]
            public async Task Providing_an_unkown_value_type_should_trigger_diagnostic(string argumentsAttributeUsage)
            {
                var testCode =
/* lang=c#-test */ $@"using BenchmarkDotNet.Attributes;

public class BenchmarkClass
{{
    [Benchmark]
    {argumentsAttributeUsage}
    public void BenchmarkMethod(byte a, bool b)
    {{
    
    }}
}}";

                TestCode = testCode;

                DisableCompilerDiagnostics();
                AddDefaultExpectedDiagnostic("dummy_literal", "byte", "<unknown>");

                await RunAsync();
            }

            public static TheoryData<string> EmptyArgumentsAttributeUsagesWithMismatchingValueCount()
            {
                return new TheoryData<string>(GenerateData());

#if NET6_0_OR_GREATER
                static IEnumerable<string> GenerateData()
#else
                IEnumerable<string> GenerateData()
#endif
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
#if NET8_0_OR_GREATER
                                                  "[Arguments({0}[]{1})]",
#endif
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

            public static IEnumerable<string> ArgumentsAttributeUsagesWithMismatchingValueCount() => GenerateAttributeUsages(new List<string> {
                                                                                                                                 "42, \"test\"",
                                                                                                                                 "\"value\", 100, true"
                                                                                                                             });

            public static TheoryData<string> ArgumentsAttributeUsagesWithMatchingValueTypes() => new TheoryData<string>(GenerateAttributeUsages(new List<string> {
                                                                                                                                                    "42, \"test\"",
                                                                                                                                                    "43, \"test2\""
                                                                                                                                                }));

            public static TheoryData<string> ArgumentsAttributeUsagesWithConvertibleValueTypes() => new TheoryData<string>(GenerateAttributeUsages(new List<string> {
                                                                                                                                                       "42, \"test\"",
                                                                                                                                                       "(byte)5, \"test2\""
                                                                                                                                                   }));

            public static TheoryData<string> ArgumentsAttributeUsagesWithMismatchingValueTypesWithLocationMarker() => new TheoryData<string>(GenerateAttributeUsages(new List<string> {
                                                                                                                                                                         "{|#0:typeof(string)|}, {|#1:\"test\"|}",
                                                                                                                                                                         "{|#2:999|}, true"
                                                                                                                                                                     }));

            public static TheoryData<string> ArgumentsAttributeUsagesWithUnknownValueTypesWithLocationMarker() => new TheoryData<string>(GenerateAttributeUsages(new List<string> {
                                                                                                                                                                     "{|#0:dummy_literal|}, true"
                                                                                                                                                                 }));
        }

        private static IEnumerable<string> GenerateAttributeUsages(List<string> valueLists)
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
#if NET8_0_OR_GREATER
                                          "[Arguments({0}[ {1} ]{2})]"
#endif
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
}
