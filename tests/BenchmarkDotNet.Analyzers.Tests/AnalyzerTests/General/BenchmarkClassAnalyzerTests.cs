namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General
{
    using Fixtures;

    using Analyzers.General;

    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

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
            public async Task Abstract_class_annotated_with_at_least_one_generictypearguments_attribute_should_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount, [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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
        }

        public class ClassWithGenericTypeArgumentsAttributeMustBeGeneric : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public ClassWithGenericTypeArgumentsAttributeMustBeGeneric() : base(BenchmarkClassAnalyzer.ClassWithGenericTypeArgumentsAttributeMustBeGenericRule) { }

            [Theory, CombinatorialData]
            public async Task Generic_class_annotated_with_a_generictypearguments_attribute_should_not_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                          [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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
                                                                                                                         [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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
                                                                                                                                                                   [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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
        }

        public class GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount() : base(BenchmarkClassAnalyzer.GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule) { }

            [Theory, CombinatorialData]
            public async Task Generic_class_annotated_with_a_generictypearguments_attribute_having_matching_type_argument_count_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                              [CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                                              [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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
                                                                                                                                                             [CombinatorialValues("", "[Benchmark]")] string benchmarkAttributeUsage)
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

        public class OnlyOneMethodCanBeBaseline : AnalyzerTestFixture<BenchmarkClassAnalyzer>
        {
            public OnlyOneMethodCanBeBaseline() : base(BenchmarkClassAnalyzer.OnlyOneMethodCanBeBaselineRule) { }

            [Fact]
            public async Task Class_with_only_one_benchmark_method_marked_as_baseline_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark(Baseline = true)]
                                                               public void BaselineBenchmarkMethod()
                                                               {

                                                               }
                                                               
                                                               [Benchmark]
                                                               public void NonBaselineBenchmarkMethod1()
                                                               {
                                                               
                                                               }
                                                               
                                                               [Benchmark(Baseline = false)]
                                                               public void NonBaselineBenchmarkMethod2()
                                                               {
                                                               
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task Class_with_no_benchmark_methods_marked_as_baseline_should_not_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark]
                                                               public void NonBaselineBenchmarkMethod1()
                                                               {

                                                               }
                                                               
                                                               [Benchmark]
                                                               public void NonBaselineBenchmarkMethod2()
                                                               {
                                                               
                                                               }
                                                               
                                                               [Benchmark(Baseline = false)]
                                                               public void NonBaselineBenchmarkMethod3()
                                                               {
                                                               
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;

                await RunAsync();
            }

            [Fact]
            public async Task Class_with_more_than_one_benchmark_method_marked_as_baseline_should_trigger_diagnostic()
            {
                const string testCode = /* lang=c#-test */ """
                                                           using BenchmarkDotNet.Attributes;

                                                           public class BenchmarkClass
                                                           {
                                                               [Benchmark({|#0:Baseline = true|})]
                                                               [Benchmark]
                                                               public void BaselineBenchmarkMethod1()
                                                               {

                                                               }
                                                               
                                                               [Benchmark]
                                                               public void NonBaselineBenchmarkMethod1()
                                                               {
                                                               
                                                               }
                                                               
                                                               [Benchmark(Baseline = false)]
                                                               public void NonBaselineBenchmarkMethod2()
                                                               {
                                                               
                                                               }
                                                               
                                                               [Benchmark({|#1:Baseline = true|})]
                                                               public void BaselineBenchmarkMethod2()
                                                               {
                                                               
                                                               }
                                                               
                                                               [Benchmark({|#2:Baseline = true|})]
                                                               [Benchmark({|#3:Baseline = true|})]
                                                               public void BaselineBenchmarkMethod3()
                                                               {
                                                               
                                                               }
                                                           }
                                                           """;

                TestCode = testCode;
                DisableCompilerDiagnostics();
                AddExpectedDiagnostic(0);
                AddExpectedDiagnostic(1);
                AddExpectedDiagnostic(2);
                AddExpectedDiagnostic(3);

                await RunAsync();
            }
        }

        public static TheoryData<int> TypeParametersListLengthTheoryData => new(TypeParametersListLengthEnumerable);

        public static IEnumerable<int> TypeParametersListLengthEnumerable => Enumerable.Range(1, TypeParametersTheoryData.Count);

        private static ReadOnlyCollection<string> TypeParametersTheoryData => Enumerable.Range(1, 3)
                                                                                        .Select(i => $"TParameter{i}")
                                                                                        .ToList()
                                                                                        .AsReadOnly();
        private static ReadOnlyCollection<string> GenericTypeArgumentsTheoryData => new List<string> { "int", "string", "bool" }.AsReadOnly();
    }
}
