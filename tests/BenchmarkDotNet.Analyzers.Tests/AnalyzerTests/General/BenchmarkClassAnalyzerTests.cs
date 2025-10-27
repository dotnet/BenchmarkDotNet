namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General
{
    using Fixtures;

    using Analyzers.General;

    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    // TODO: Verify which diagnostics rely on presence of [Benchmark] attribute on methods and test with 0, 2, or 3 attribute usages

    public class BenchmarkClassAnalyzerTests
    {
        public class General : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            [Theory]
            [InlineData("")]
            [InlineData(" abstract")]
            public async Task Class_not_annotated_with_any_generictypearguments_attributes_and_with_no_methods_annotated_with_benchmark_attribute_should_not_trigger_diagnostic(string abstractModifier)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public{{abstractModifier}} class BenchmarkClass
                                                    {
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                await RunAsync();
            }
        }

        public class ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract() : base(BenchmarkClassAnalyzer.ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule) { }

            [Theory, CombinatorialData]
            public async Task Abstract_class_annotated_with_at_least_one_generictypearguments_attribute_should_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                  [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var genericTypeArgumentsAttributeUsages = Enumerable.Repeat("[GenericTypeArguments(typeof(int))]", genericTypeArgumentsAttributeUsageCount);

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    {{string.Join("\n", genericTypeArgumentsAttributeUsages)}}
                                                    public {|#0:abstract|} class {{benchmarkClassName}}<TParameter>
                                                    {
                                                        {{benchmarkAttributeUsage}}
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            public static IEnumerable<string> BenchmarkAttributeUsagesEnumerableLocal => BenchmarkAttributeUsagesEnumerable;
        }

        public class ClassWithGenericTypeArgumentsAttributeMustBeGeneric : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public ClassWithGenericTypeArgumentsAttributeMustBeGeneric() : base(BenchmarkClassAnalyzer.ClassWithGenericTypeArgumentsAttributeMustBeGenericRule) { }

            [Theory, CombinatorialData]
            public async Task Generic_class_annotated_with_a_generictypearguments_attribute_should_not_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                          [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var genericTypeArgumentsAttributeUsages = Enumerable.Repeat("[GenericTypeArguments(typeof(int))]", genericTypeArgumentsAttributeUsageCount);

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    {{string.Join("\n", genericTypeArgumentsAttributeUsages)}}
                                                    public class BenchmarkClass<TParameter>
                                                    {
                                                        {{benchmarkAttributeUsage}}
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Nongeneric_class_annotated_with_a_generictypearguments_attribute_should_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                         [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var genericTypeArgumentsAttributeUsages = Enumerable.Repeat("[GenericTypeArguments(typeof(int))]", genericTypeArgumentsAttributeUsageCount);

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                   
                                                    {{string.Join("\n", genericTypeArgumentsAttributeUsages)}}
                                                    public class {|#0:{{benchmarkClassName}}|}
                                                    {
                                                        {{benchmarkAttributeUsage}}
                                                        public void BenchmarkMethod()
                                                        {
                                                   
                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Nongeneric_class_annotated_with_a_generictypearguments_attribute_inheriting_from_an_abstract_generic_class_should_trigger_diagnostic([CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                                   [CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                                                   [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var genericTypeArguments = string.Join(", ", GenericTypeArguments.Select(ta => $"typeof({ta})").Take(typeParametersListLength));
                var genericTypeArgumentsAttributeUsages = Enumerable.Repeat($"[GenericTypeArguments({genericTypeArguments})]", genericTypeArgumentsAttributeUsageCount);

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    {{string.Join("\n", genericTypeArgumentsAttributeUsages)}}
                                                    public class {|#0:{{benchmarkClassName}}|} : BenchmarkClassBase<{{string.Join(", ", GenericTypeArguments.Take(typeParametersListLength))}}>
                                                    {
                                                    }
                                                    """;

                var benchmarkBaseClassDocument = /* lang=c#-test */ $$"""
                                                                      using BenchmarkDotNet.Attributes;

                                                                      public abstract class BenchmarkClassBase<{{string.Join(", ", TypeParameters.Take(typeParametersListLength))}}>
                                                                      {
                                                                          {{benchmarkAttributeUsage}}
                                                                          public void BenchmarkMethod()
                                                                          {

                                                                          }
                                                                      }
                                                                      """;

                TestCode = testCode;
                AddSource(benchmarkBaseClassDocument);

                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            public static IEnumerable<int> TypeParametersListLengthEnumerableLocal => TypeParametersListLengthEnumerable;

            private static ReadOnlyCollection<string> TypeParameters => TypeParametersTheoryData;

            private static ReadOnlyCollection<string> GenericTypeArguments => GenericTypeArgumentsTheoryData;

            public static IEnumerable<string> BenchmarkAttributeUsagesEnumerableLocal => BenchmarkAttributeUsagesEnumerable;
        }

        public class GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount() : base(BenchmarkClassAnalyzer.GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule) { }

            [Theory, CombinatorialData]
            public async Task Generic_class_annotated_with_a_generictypearguments_attribute_having_matching_type_argument_count_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                              [CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                                              [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var genericTypeArguments = string.Join(", ", GenericTypeArguments.Select(ta => $"typeof({ta})").Take(typeParametersListLength));
                var genericTypeArgumentsAttributeUsages = Enumerable.Repeat($"[GenericTypeArguments({genericTypeArguments})]", genericTypeArgumentsAttributeUsageCount);

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    {{string.Join("\n", genericTypeArgumentsAttributeUsages)}}
                                                    public class BenchmarkClass<{{string.Join(", ", TypeParameters.Take(typeParametersListLength))}}>
                                                    {
                                                        {{benchmarkAttributeUsage}}
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Generic_class_annotated_with_a_generictypearguments_attribute_having_mismatching_type_argument_count_should_trigger_diagnostic([CombinatorialMemberData(nameof(TypeArgumentsData))] ValueTupleDouble<string, int> typeArgumentsData,
                                                                                                                                                             [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    [GenericTypeArguments({|#0:{{typeArgumentsData.Value1}}|})]
                                                    [GenericTypeArguments(typeof(int))]
                                                    public class {{benchmarkClassName}}<T1>
                                                    {
                                                        {{benchmarkAttributeUsage}}
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(1, "", benchmarkClassName, typeArgumentsData.Value2);

                await RunAsync();
            }

            public static IEnumerable<ValueTupleDouble<string, int>> TypeArgumentsData =>
            [
                ("typeof(int), typeof(string)", 2),
                ("typeof(int), typeof(string), typeof(bool)", 3)
            ];

            public static IEnumerable<int> TypeParametersListLengthEnumerableLocal => TypeParametersListLengthEnumerable;

            private static ReadOnlyCollection<string> TypeParameters => TypeParametersTheoryData;

            private static ReadOnlyCollection<string> GenericTypeArguments => GenericTypeArgumentsTheoryData;

            public static IEnumerable<string> BenchmarkAttributeUsagesEnumerableLocal => BenchmarkAttributeUsagesEnumerable;
        }

        public class MethodMustBePublic : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public MethodMustBePublic() : base(BenchmarkClassAnalyzer.MethodMustBePublicRule) { }

            [Fact]
            public async Task Public_method_annotated_with_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark]
                                                               public void BenchmarkMethod()
                                                               {

                                                               }
                                                               
                                                               public void NonBenchmarkMethod()
                                                               {
                                                               
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [ClassData(typeof(NonPublicClassMemberAccessModifiersTheoryData))]
            public async Task Nonpublic_method_annotated_with_benchmark_attribute_should_trigger_diagnostic(string nonPublicClassAccessModifier)
            {
                const string benchmarkMethodName = "BenchmarkMethod";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        {{nonPublicClassAccessModifier}}void {|#0:{{benchmarkMethodName}}|}()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(benchmarkMethodName);

                await RunAsync();
            }
        }

        public class MethodMustBeNonGeneric : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public MethodMustBeNonGeneric() : base(BenchmarkClassAnalyzer.MethodMustBeNonGenericRule) { }

            [Fact]
            public async Task Nongeneric_method_annotated_with_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;
                                                           
                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark]
                                                               public void NonGenericBenchmarkMethod()
                                                               {
                                                           
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task Generic_method_not_annotated_with_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               public void GenericMethod<T>()
                                                               {

                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Theory]
            [MemberData(nameof(TypeParametersListLength))]
            public async Task Nongeneric_method_annotated_with_benchmark_attribute_should_trigger_diagnostic(int typeParametersListLength)
            {
                const string benchmarkMethodName = "GenericBenchmarkMethod";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass
                                                    {
                                                        [Benchmark]
                                                        public void {{benchmarkMethodName}}{|#0:<{{string.Join(", ", TypeParameters.Take(typeParametersListLength))}}>|}()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(benchmarkMethodName);

                await RunAsync();
            }

            public static TheoryData<int> TypeParametersListLength => TypeParametersListLengthTheoryData;

            private static ReadOnlyCollection<string> TypeParameters => TypeParametersTheoryData;
        }

        public class ClassMustBeNonStatic : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public ClassMustBeNonStatic() : base(BenchmarkClassAnalyzer.ClassMustBeNonStaticRule) { }

            [Fact]
            public async Task Instance_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark]
                                                               public void BenchmarkMethod()
                                                               {

                                                               }
                                                               
                                                               public void NonBenchmarkMethod()
                                                               {
                                                               
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task Static_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic()
            {
                const string benchmarkClassName = "BenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Attributes;
                                                           
                                                             public {|#0:static|} class {{benchmarkClassName}}
                                                             {
                                                                 [Benchmark]
                                                                 public static void BenchmarkMethod()
                                                                 {
                                                           
                                                                 }
                                                             }
                                                             """;

                TestCode = testCode;
                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }
        }

        public class SingleNullArgumentToBenchmarkCategoryAttributeNotAllowed : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public SingleNullArgumentToBenchmarkCategoryAttributeNotAllowed() : base(BenchmarkClassAnalyzer.SingleNullArgumentToBenchmarkCategoryAttributeNotAllowedRule)
            {
            }

            [Theory, CombinatorialData]
            public async Task Providing_a_non_null_single_argument_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                 bool useConstantFromOtherClass,
                                                                                                 bool useLocalConstant,
                                                                                                 [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    [assembly: BenchmarkDotNet.Attributes.BenchmarkCategory({{(useConstantFromOtherClass ? "Constants.Value" : "\"test\"")}})]
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                    }
                                                    """;

                var benchmarkCategoryAttributeUsage = $"[BenchmarkCategory({(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : "\"test\"")})]";

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           {{benchmarkCategoryAttributeUsage}}
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1
                                                                           {
                                                                               {{(useLocalConstant ? $"private const string _x = {(useConstantFromOtherClass ? "Constants.Value" : "\"test\"")};" : "")}}
                                                                           
                                                                               {{benchmarkCategoryAttributeUsage}}
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                               
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                ReferenceConstants("string", "\"test\"");

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_an_empty_array_argument_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                              [CombinatorialMemberData(nameof(EmptyBenchmarkCategoryAttributeArgumentEnumerableLocal))] string emptyBenchmarkCategoryAttributeArgument,
                                                                                              [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    [assembly: BenchmarkDotNet.Attributes.BenchmarkCategory{{emptyBenchmarkCategoryAttributeArgument}}]
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           [BenchmarkCategory{{emptyBenchmarkCategoryAttributeArgument}}]
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1
                                                                           {
                                                                               [BenchmarkCategory{{emptyBenchmarkCategoryAttributeArgument}}]
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                               
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_an_array_argument_containing_one_or_more_null_values_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                           bool useConstantsFromOtherClass,
                                                                                                                           bool useLocalConstants,
                                                                                                                           [CombinatorialValues("{0}", "{0}, {1}", "{1}, {0}", "{0}, {1}, {0}", "{1}, {0}, {1}")] string valuesTemplate,
                                                                                                                           [CombinatorialMemberData(nameof(BenchmarkCategoryAttributeValuesContainerEnumerableLocal), false)] string valuesContainer,
                                                                                                                           [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var assemblyLevelAttributeValues = string.Format(valuesContainer, string.Format(valuesTemplate,
                                                                                                useConstantsFromOtherClass ? "Constants.Value1" : "null",
                                                                                                useConstantsFromOtherClass ? "Constants.Value2" : "\"test\""));

                var testCode = /* lang=c#-test */ $$"""
                                                    [assembly: BenchmarkDotNet.Attributes.BenchmarkCategory({{assemblyLevelAttributeValues}})]
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                    }
                                                    """;

                var classAndMethodAttributeLevelValues = string.Format(valuesContainer, string.Format(valuesTemplate,
                                                                                                      useLocalConstants ? "_xNull" : useConstantsFromOtherClass ? "Constants.Value1" : "null",
                                                                                                      useLocalConstants ? "_xValue" : useConstantsFromOtherClass ? "Constants.Value2" : "\"test\""));

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           [BenchmarkCategory({{classAndMethodAttributeLevelValues}})]
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1
                                                                           {
                                                                               {{(useLocalConstants ? $"""
                                                                                                       private const string _xNull = {(useConstantsFromOtherClass ? "Constants.Value1" : "null")};
                                                                                                       private const string _xValue = {(useConstantsFromOtherClass ? "Constants.Value2" : "\"test\"")};
                                                                                                       """ : "")}}
                                                                           
                                                                               [BenchmarkCategory({{classAndMethodAttributeLevelValues}})]
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                               
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                ReferenceConstants(("string", "null"), ("string", "\"test\""));

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Providing_a_null_single_argument_should_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                         bool useConstantFromOtherClass,
                                                                                         bool useLocalConstant,
                                                                                         [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    [assembly: BenchmarkDotNet.Attributes.BenchmarkCategory({|#0:{{(useConstantFromOtherClass ? "Constants.Value" : "null")}}|})]
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           [BenchmarkCategory({|#1:{{(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : "null")}}|})]
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1
                                                                           {
                                                                               {{(useLocalConstant ? $"private const string _x = {(useConstantFromOtherClass ? "Constants.Value" : "null")};" : "")}}
                                                                           
                                                                               [BenchmarkCategory({|#2:{{(useLocalConstant ? "_x" : useConstantFromOtherClass ? "Constants.Value" : "null")}}|})]
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                               
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                ReferenceConstants("string", "null");

                AddExpectedDiagnostic(0);
                AddExpectedDiagnostic(1);
                AddExpectedDiagnostic(2);

                await RunAsync();
            }

            public static IEnumerable<string> ClassAbstractModifiersEnumerableLocal => ClassAbstractModifiersEnumerable;

            public static IEnumerable<string> BenchmarkAttributeUsagesEnumerableLocal => BenchmarkAttributeUsagesEnumerable;

            public static IEnumerable<string> EmptyBenchmarkCategoryAttributeArgumentEnumerableLocal => EmptyBenchmarkCategoryAttributeArgumentEnumerable();

            public static IEnumerable<string> BenchmarkCategoryAttributeValuesContainerEnumerableLocal(bool useParamsValues) => BenchmarkCategoryAttributeValuesContainerEnumerable(useParamsValues);
        }

        public class OnlyOneMethodCanBeBaseline : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public OnlyOneMethodCanBeBaseline() : base(BenchmarkClassAnalyzer.OnlyOneMethodCanBeBaselineRule) { }

            // TODO: Test with duplicate [Benchmark] attribute usage on same method (should not trigger diagnostic)
            //  Category can contain multiple values separated by comma
            //  Test with all types of array containers (see Parameter attribute tests)

            [Theory, CombinatorialData]
            public async Task Class_with_only_one_benchmark_method_marked_as_baseline_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                    bool useConstantsFromOtherClass,
                                                                                                                    bool useLocalConstants,
                                                                                                                    bool useInvalidFalseValue)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")};
                                                                               """ : "")}}
                                                    
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                    
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2, System.IEquatable<BenchmarkClassAncestor1>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2 : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")};" : "")}}
                                                                           
                                                                               [Benchmark(Baseline = {{(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                                               public void NonBaselineBenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               public void BenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               private void BenchmarkMethod3()
                                                                               {
                                                                                                                                                      
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);
                ReferenceConstants(("bool", "true"), ("bool", useInvalidFalseValue ? "dummy" : "false"));

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_duplicated_benchmark_attribute_usages_per_method_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                        bool useConstantsFromOtherClass,
                                                                                                                        bool useLocalConstants,
                                                                                                                        [CombinatorialValues(2, 3)] int baselineBenchmarkAttributeUsageCount)
            {
                var baselineBenchmarkAttributeUsages = string.Join("\n", Enumerable.Repeat($"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]", baselineBenchmarkAttributeUsageCount));

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        {{baselineBenchmarkAttributeUsages}}
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                    
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2, System.IEquatable<BenchmarkClassAncestor1>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2 : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};" : "")}}
                                                                           
                                                                               {{baselineBenchmarkAttributeUsages}}
                                                                               public void NonBaselineBenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               public void BenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               private void BenchmarkMethod3()
                                                                               {
                                                                                                                                                      
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);
                ReferenceConstants(("bool", "true"), ("bool", "false"));

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_no_benchmark_methods_marked_as_baseline_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                               bool useConstantFromOtherClass,
                                                                                                               bool useLocalConstant,
                                                                                                               bool useInvalidFalseValue)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstant ? $"private const bool _xFalse = {(useConstantFromOtherClass ? "Constants.Value" : useInvalidFalseValue ? "dummy" : "false")};" : "")}}
                                                    
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {

                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               [Benchmark(Baseline = {{(useLocalConstant ? "_xFalse" : useConstantFromOtherClass ? "Constants.Value" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                                               public void NonBaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants("bool", useInvalidFalseValue ? "dummy" : "false");
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_per_unique_category_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                             bool useConstantsFromOtherClass,
                                                                                                                                             bool useLocalConstants,
                                                                                                                                             [CombinatorialMemberData(nameof(BenchmarkCategoryAttributeValuesContainerEnumerableLocal), true)] string valuesContainer)
            {
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            null, "test", null, "TEST", "test2"
                                                                                                            """)}})]
                                                        {{baselineBenchmarkAttributeUsage}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        

                                                        [BenchmarkCategory({{string.Format(valuesContainer, "null, null")}})]
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            "test", null
                                                                                                            """)}})]
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            "test2"
                                                                                                            """)}})]
                                                        {{baselineBenchmarkAttributeUsage}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, "null, null")}})]
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                                                   "test", null
                                                                                                                                   """)}})]
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                                                   "test2"
                                                                                                                                   """)}})]
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_should_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                     bool useConstantsFromOtherClass,
                                                                                                                     bool useLocalConstants,
                                                                                                                     bool useDuplicateInSameClass)
            {
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var baselineBenchmarkAttributeUsageWithLocationMarker = $"[Benchmark({{{{|#{{0}}:Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}|}}}})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        {{string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 0)}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        {{(useDuplicateInSameClass ? string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 1) : "")}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                AddExpectedDiagnostic(0);

                if (useDuplicateInSameClass)
                {
                    AddExpectedDiagnostic(1);
                }

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_with_empty_category_should_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                         bool useConstantsFromOtherClass,
                                                                                                                                         bool useLocalConstants,
                                                                                                                                         [CombinatorialMemberData(nameof(EmptyBenchmarkCategoryAttributeEnumerableLocal))] string emptyBenchmarkCategoryAttribute,
                                                                                                                                         bool useDuplicateInSameClass)
            {
                var emptyBenchmarkCategoryAttributeUsages = string.Join("\n", Enumerable.Repeat(emptyBenchmarkCategoryAttribute, 3));
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var baselineBenchmarkAttributeUsageWithLocationMarker = $"[Benchmark({{{{|#{{0}}:Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}|}}}})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        {{emptyBenchmarkCategoryAttributeUsages}}
                                                        {{string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 0)}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        {{emptyBenchmarkCategoryAttributeUsages}}
                                                        {{(useDuplicateInSameClass ? string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 1) : "")}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               {{emptyBenchmarkCategoryAttributeUsages}}
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                AddExpectedDiagnostic(0);

                if (useDuplicateInSameClass)
                {
                    AddExpectedDiagnostic(1);
                }

                await RunAsync();
            }

            public static IEnumerable<string> ClassAbstractModifiersEnumerableLocal => ClassAbstractModifiersEnumerable;

            public static IEnumerable<string> BenchmarkCategoryAttributeValuesContainerEnumerableLocal(bool useParamsValues) => BenchmarkCategoryAttributeValuesContainerEnumerable(useParamsValues);

            public static IEnumerable<string> EmptyBenchmarkCategoryAttributeEnumerableLocal => EmptyBenchmarkCategoryAttributeEnumerable();
        }

        public class OnlyOneMethodCanBeBaselinePerCategory : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public OnlyOneMethodCanBeBaselinePerCategory() : base(BenchmarkClassAnalyzer.OnlyOneMethodCanBeBaselinePerCategoryRule) { }

            [Theory, CombinatorialData]
            public async Task Class_with_only_one_benchmark_method_marked_as_baseline_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                    bool useConstantsFromOtherClass,
                                                                                                                    bool useLocalConstants,
                                                                                                                    bool useInvalidFalseValue)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")};
                                                                               """ : "")}}
                                                    
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                    
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2, System.IEquatable<BenchmarkClassAncestor1>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2 : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")};" : "")}}
                                                                           
                                                                               [Benchmark(Baseline = {{(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                                               public void NonBaselineBenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               public void BenchmarkMethod2()
                                                                               {

                                                                               }
                                                                               
                                                                               private void BenchmarkMethod3()
                                                                               {
                                                                                                                                                      
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);
                ReferenceConstants(("bool", "true"), ("bool", useInvalidFalseValue ? "dummy" : "false"));

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_only_one_benchmark_method_marked_as_baseline_per_unique_category_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                        bool useConstantsFromOtherClass,
                                                                                                                                        bool useLocalConstants,
                                                                                                                                        bool useInvalidFalseValue)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")};
                                                                               """ : "")}}
                                                    
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                    
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category2")]
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category2")]
                                                        [Benchmark]
                                                        public void BaselineBenchmarkMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                        
                                                        }
                                                        
                                                        [Benchmark(Baseline = {{(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               [Benchmark(Baseline = {{(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}})]
                                                                               public void NonBaselineBenchmarkMethod2()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", useInvalidFalseValue ? "dummy" : "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_no_benchmark_methods_marked_as_baseline_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                               bool useConstantFromOtherClass,
                                                                                                               bool useLocalConstant,
                                                                                                               bool useInvalidFalseValue)
            {
                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstant ? $"private const bool _xFalse = {(useConstantFromOtherClass ? "Constants.Value" : useInvalidFalseValue ? "dummy" : "false")};" : "")}}
                                                    
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {

                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               [Benchmark(Baseline = {{(useLocalConstant ? "_xFalse" : useConstantFromOtherClass ? "Constants.Value" : useInvalidFalseValue ? "dummy" : "false")}})]
                                                                               public void NonBaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants("bool", useInvalidFalseValue ? "dummy" : "false");
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                DisableCompilerDiagnostics();

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_should_trigger_not_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                         bool useConstantsFromOtherClass,
                                                                                                                         bool useLocalConstants,
                                                                                                                         bool useDuplicateInSameClass)
            {
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        {{baselineBenchmarkAttributeUsage}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        {{(useDuplicateInSameClass ? baselineBenchmarkAttributeUsage : "")}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_with_empty_category_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                             bool useConstantsFromOtherClass,
                                                                                                                                             bool useLocalConstants,
                                                                                                                                             [CombinatorialMemberData(nameof(EmptyBenchmarkCategoryAttributeEnumerableLocal))] string emptyBenchmarkCategoryAttribute,
                                                                                                                                             bool useDuplicateInSameClass)
            {
                var emptyBenchmarkCategoryAttributeUsages = string.Join("\n", Enumerable.Repeat(emptyBenchmarkCategoryAttribute, 3));
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        {{emptyBenchmarkCategoryAttributeUsages}}
                                                        {{baselineBenchmarkAttributeUsage}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        {{emptyBenchmarkCategoryAttributeUsages}}
                                                        {{(useDuplicateInSameClass ? baselineBenchmarkAttributeUsage : "")}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               {{emptyBenchmarkCategoryAttributeUsages}}
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_per_unique_category_should_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                         bool useConstantsFromOtherClass,
                                                                                                                                         bool useLocalConstants,
                                                                                                                                         [CombinatorialMemberData(nameof(BenchmarkCategoryAttributeValuesContainerEnumerableLocal), true)] string valuesContainer,
                                                                                                                                         bool useDuplicateInSameClass)
            {
                var baselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")})]";
                var baselineBenchmarkAttributeUsageWithLocationMarker = $"[Benchmark({{{{|#{{0}}:Baseline = {(useLocalConstants ? "_xTrue" : useConstantsFromOtherClass ? "Constants.Value1" : "true")}|}}}})]";
                var nonBaselineBenchmarkAttributeUsage = $"[Benchmark(Baseline = {(useLocalConstants ? "_xFalse" : useConstantsFromOtherClass ? "Constants.Value2" : "false")})]";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    
                                                    public class BenchmarkClass : BenchmarkClassAncestor1
                                                    {
                                                        {{(useLocalConstants ? $"""
                                                                               private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};
                                                                               private const bool _xFalse = {(useConstantsFromOtherClass ? "Constants.Value2" : "false")};
                                                                               """ : "")}}
                                                    
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            null, "test", null, "TEST", "test2"
                                                                                                            """)}})]
                                                        {{string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 0)}}
                                                        public void BaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        

                                                        [BenchmarkCategory({{string.Format(valuesContainer, "null, null")}})]
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            "test", null
                                                                                                            """)}})]
                                                        [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                            "test2"
                                                                                                            """)}})]
                                                        {{(useDuplicateInSameClass ? string.Format(baselineBenchmarkAttributeUsageWithLocationMarker, 1) : "")}}
                                                        public void BaselineBenchmarkMethod2()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod1()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [BenchmarkCategory("Category1")]
                                                        public void DummyMethod()
                                                        {
                                                                                                            
                                                        }
                                                        
                                                        [Benchmark]
                                                        public void NonBaselineBenchmarkMethod2()
                                                        {
                                                        
                                                        }
                                                        
                                                        {{nonBaselineBenchmarkAttributeUsage}}
                                                        public void NonBaselineBenchmarkMethod3()
                                                        {
                                                        
                                                        }
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2
                                                                           {
                                                                               {{(useLocalConstants ? $"private const bool _xTrue = {(useConstantsFromOtherClass ? "Constants.Value1" : "true")};" : "")}}
                                                                           
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, "null, null")}})]
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                                                   "test", null
                                                                                                                                   """)}})]
                                                                               [BenchmarkCategory({{string.Format(valuesContainer, """
                                                                                                                                   "test2"
                                                                                                                                   """)}})]
                                                                               {{baselineBenchmarkAttributeUsage}}
                                                                               public void BaselineBenchmarkMethod3()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                ReferenceConstants(("bool", "true"), ("bool", "false"));
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                AddExpectedDiagnostic(0);

                if (useDuplicateInSameClass)
                {
                    AddExpectedDiagnostic(1);
                }

                await RunAsync();
            }

            public static IEnumerable<string> ClassAbstractModifiersEnumerableLocal => ClassAbstractModifiersEnumerable;

            public static IEnumerable<string> EmptyBenchmarkCategoryAttributeEnumerableLocal => EmptyBenchmarkCategoryAttributeEnumerable();

            public static IEnumerable<string> BenchmarkCategoryAttributeValuesContainerEnumerableLocal(bool useParamsValues) => BenchmarkCategoryAttributeValuesContainerEnumerable(useParamsValues);
        }

        public static TheoryData<int> TypeParametersListLengthTheoryData => new(TypeParametersListLengthEnumerable);

        public static IEnumerable<int> TypeParametersListLengthEnumerable => Enumerable.Range(1, TypeParametersTheoryData.Count);

        private static ReadOnlyCollection<string> TypeParametersTheoryData => Enumerable.Range(1, 3)
                                                                                        .Select(i => $"TParameter{i}")
                                                                                        .ToList()
                                                                                        .AsReadOnly();
        private static ReadOnlyCollection<string> GenericTypeArgumentsTheoryData => new List<string> { "int", "string", "bool" }.AsReadOnly();

        public static IEnumerable<string> ClassAbstractModifiersEnumerable => [ "", "abstract " ];

        public static IEnumerable<string> BenchmarkAttributeUsagesEnumerable => [ "", "[Benchmark] " ];

        //TODO: Move to a common helper class
        public static IEnumerable<string> EmptyBenchmarkCategoryAttributeArgumentEnumerable()
        {
            yield return "";
            yield return "()";

            var nameColonUsages = new List<string>
                                  {
                                      "",
                                      "categories: "
                                  };

            var attributeUsagesBase = new List<string>
                                      {
                                          "({0}new string[] {{ }})",
                                          "({0}new string[0])",
                                          "({0}[ ])"
                                      };

            foreach (var attributeUsageBase in attributeUsagesBase)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    yield return string.Format(attributeUsageBase, nameColonUsage);
                }
            }
        }

        public static IEnumerable<string> EmptyBenchmarkCategoryAttributeEnumerable()
        {
            yield return "[BenchmarkCategory]";
            yield return "[BenchmarkCategory()]";

            var nameColonUsages = new List<string>
                                  {
                                      "",
                                      "categories: "
                                  };

            var attributeUsagesBase = new List<string>
                                      {
                                          "({0}new string[] {{ }})",
                                          "({0}new string[0])",
                                          "({0}[ ])"
                                      };

            foreach (var attributeUsageBase in attributeUsagesBase)
            {
                foreach (var nameColonUsage in nameColonUsages)
                {
                    yield return $"[BenchmarkCategory{string.Format(attributeUsageBase, nameColonUsage)}]";
                }
            }
        }

        public static IEnumerable<string> BenchmarkCategoryAttributeValuesContainerEnumerable(bool useParamsValues)
        {
            return GenerateData(useParamsValues).Distinct();

            static IEnumerable<string> GenerateData(bool useParamsValues)
            {
                var nameColonUsages = new List<string>
                                      {
                                          "",
                                          "categories: "
                                      };

                List<string> attributeUsagesBase = useParamsValues ? [ "{{0}}" ] : [ ];

                attributeUsagesBase.AddRange([
                                                "{0}new string[] {{{{ {{0}} }}}}",
                                                "{0}[ {{0}} ]"
                                             ]);

                foreach (var attributeUsageBase in attributeUsagesBase)
                {
                    foreach (var nameColonUsage in nameColonUsages)
                    {
                        yield return string.Format(attributeUsageBase, nameColonUsage);
                    }
                }
            }
        }
    }
}
