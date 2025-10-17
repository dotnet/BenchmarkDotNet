namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.BenchmarkRunner
{
    using Fixtures;

    using Analyzers.BenchmarkRunner;

    using Xunit;

    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    public class RunAnalyzerTests
    {
        public class General : AnalyzerTestFixture<RunAnalyzer>
        {
            [Theory, CombinatorialData]
            public async Task Invoking_with_a_public_nonabstract_unsealed_nongeneric_type_argument_class_having_only_one_and_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic(bool isGenericInvocation)
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var invocationExpression = isGenericInvocation ? $"<{classWithOneBenchmarkMethodName}>()" : $"(typeof({classWithOneBenchmarkMethodName}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;

                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public class {{classWithOneBenchmarkMethodName}}
                                                                           {
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
                                                                               {

                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassDocument);

                await RunAsync();
            }
        }

        public class TypeArgumentClassMissingBenchmarkMethods : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMissingBenchmarkMethods() : base(RunAnalyzer.TypeArgumentClassMissingBenchmarkMethodsRule) { }

            [Theory, CombinatorialData]
            public async Task Invoking_with_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic(bool isGenericInvocation)
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var invocationExpression = isGenericInvocation ? $"<{classWithOneBenchmarkMethodName}>()" : $"(typeof({classWithOneBenchmarkMethodName}))";

                var testCode = /* lang=c#-test */ $$"""
                                                     using BenchmarkDotNet.Running;
                                                   
                                                     public class Program
                                                     {
                                                         public static void Main(string[] args) {
                                                             BenchmarkRunner.Run{{invocationExpression}};
                                                         }
                                                     }
                                                     """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                         
                                                                           public class {{classWithOneBenchmarkMethodName}}
                                                                           {
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
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
                AddSource(benchmarkClassDocument);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_in_one_of_its_ancestor_classes_should_not_trigger_diagnostic(bool isGenericInvocation, [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier)
            {
                const string classWithOneBenchmarkMethodName = "TopLevelBenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{classWithOneBenchmarkMethodName}>()" : $"(typeof({classWithOneBenchmarkMethodName}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;

                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                         
                                                                           public class {{classWithOneBenchmarkMethodName}} : BenchmarkClassAncestor1
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
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
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
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
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_generic_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_in_one_of_its_ancestor_classes_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                                                                                [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier)
            {
                var typeParameters = string.Join(", ", TypeParameters.Take(typeParametersListLength));

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                    
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run(typeof(BenchmarkClass<{{new string(',', typeParametersListLength - 1)}}>));
                                                        }
                                                    }
                                                    """;

                var benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                  public class BenchmarkClass<{{typeParameters}}> : BenchmarkClassAncestor1<{{typeParameters}}>
                                                                  {
                                                                  }
                                                                  """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1<{{typeParameters}}> : BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2<{{typeParameters}}> : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
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
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_type_argument_class_having_no_public_method_annotated_with_the_benchmark_attribute_in_one_of_its_ancestor_classes_should_trigger_diagnostic(bool isGenericInvocation, [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier)
            {
                const string classWithOneBenchmarkMethodName = "TopLevelBenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{classWithOneBenchmarkMethodName}|}}>()" : $"(typeof({{|#0:{classWithOneBenchmarkMethodName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;

                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                         
                                                                           public class {{classWithOneBenchmarkMethodName}} : BenchmarkClassAncestor1
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2 : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               public void BenchmarkMethod()
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
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);

                AddDefaultExpectedDiagnostic(classWithOneBenchmarkMethodName);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_generic_type_argument_class_having_no_public_method_annotated_with_the_benchmark_attribute_in_one_of_its_ancestor_classes_should_trigger_diagnostic([CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                                                                  [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier)
            {
                const string classWithOneBenchmarkMethodName = "BenchmarkClass";

                var unboundGenericTypeParameterList = new string(',', typeParametersListLength - 1);
                var typeParameters = string.Join(", ", TypeParameters.Take(typeParametersListLength));

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run(typeof({|#0:{{classWithOneBenchmarkMethodName}}<{{unboundGenericTypeParameterList}}>|}));
                                                        }
                                                    }
                                                    """;

                var benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                  public class {{classWithOneBenchmarkMethodName}}<{{typeParameters}}> : BenchmarkClassAncestor1<{{typeParameters}}>
                                                                  {
                                                                  }
                                                                  """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1<{{typeParameters}}> : BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2<{{typeParameters}}> : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               public void BenchmarkMethod()
                                                                               {

                                                                               }
                                                                               
                                                                               private void BenchmarkMethod2()
                                                                               {
                                                                                                                                                      
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);
                AddSource(benchmarkClassAncestor3Document);

                AddDefaultExpectedDiagnostic($"{classWithOneBenchmarkMethodName}<{unboundGenericTypeParameterList}>");

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_type_argument_class_having_no_public_method_annotated_with_the_benchmark_attribute_should_trigger_diagnostic(bool isGenericInvocation)
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{classWithOneBenchmarkMethodName}|}}>()" : $"(typeof({{|#0:{classWithOneBenchmarkMethodName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           public class {{classWithOneBenchmarkMethodName}}
                                                                           {
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                         
                                                                               }
                                                                           }
                                                                           """;
                TestCode = testCode;
                AddSource(benchmarkClassDocument);

                AddDefaultExpectedDiagnostic(classWithOneBenchmarkMethodName);

                await RunAsync();
            }

            public static IEnumerable<string> ClassAbstractModifiersEnumerableLocal => ClassAbstractModifiersEnumerable;

            public static IEnumerable<int> TypeParametersListLengthEnumerableLocal => TypeParametersListLengthEnumerable;
        }

        public class TypeArgumentClassMustBePublic : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBePublic() : base(RunAnalyzer.TypeArgumentClassMustBePublicRule) { }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_nonpublic_class_with_multiple_inheritance_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic(bool isGenericInvocation)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{benchmarkClassName}|}}>()" : $"(typeof({{|#0:{benchmarkClassName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Attributes;
                                                             using BenchmarkDotNet.Running;

                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run{{invocationExpression}};
                                                                 }
                                                                 
                                                                 private class {{benchmarkClassName}} : BenchmarkClassAncestor1
                                                                 {
                                                                 }
                                                                 
                                                                 private class BenchmarkClassAncestor1 : BenchmarkClassAncestor2
                                                                 {
                                                                     [Benchmark]
                                                                     public void BenchmarkMethod()
                                                                     {
                                                                 
                                                                     }
                                                                 }
                                                             }
                                                             """;

                const string benchmarkClassAncestor2Document = /* lang=c#-test */ """
                                                                                  using BenchmarkDotNet.Attributes;
                                                                                  
                                                                                  public class BenchmarkClassAncestor2
                                                                                  {
                                                                                  }
                                                                                  """;



                TestCode = testCode;
                AddSource(benchmarkClassAncestor2Document);

                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_nonpublic_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic([CombinatorialMemberData(nameof(NonPublicClassAccessModifiersExceptFile))] string nonPublicClassAccessModifier,
                                                                                                                                                          bool isGenericInvocation)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{benchmarkClassName}|}}>()" : $"(typeof({{|#0:{benchmarkClassName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                        
                                                        {{nonPublicClassAccessModifier}}class {{benchmarkClassName}}
                                                        {
                                                            [Benchmark]
                                                            public void BenchmarkMethod()
                                                            {
                                                                                                                          
                                                            }
                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_file_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic(bool isGenericInvocation)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{benchmarkClassName}|}}>()" : $"(typeof({{|#0:{benchmarkClassName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    using BenchmarkDotNet.Running;

                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    
                                                    file class {{benchmarkClassName}}
                                                    {
                                                        [Benchmark]
                                                        public void BenchmarkMethod()
                                                        {

                                                        }
                                                    }
                                                    """;

                TestCode = testCode;

                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }

            public static IEnumerable<string> NonPublicClassAccessModifiersExceptFile => new NonPublicClassAccessModifiersTheoryData().Where<string>(m => m != "file ");
        }

        public class TypeArgumentClassMustBeUnsealed : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBeUnsealed() : base(RunAnalyzer.TypeArgumentClassMustBeUnsealedRule) { }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_sealed_benchmark_class_should_trigger_diagnostic(bool isGenericInvocation)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{benchmarkClassName}|}}>()" : $"(typeof({{|#0:{benchmarkClassName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           public sealed class {{benchmarkClassName}}
                                                                           {
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                         
                                                                               }
                                                                           }
                                                                           """;
                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }
        }

        public class TypeArgumentClassMustBeNonAbstract : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBeNonAbstract() : base(RunAnalyzer.TypeArgumentClassMustBeNonAbstractRule) { }

            [Theory, CombinatorialData]
            public async Task Invoking_with_an_abstract_benchmark_class_should_trigger_diagnostic(bool isGenericInvocation)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{{|#0:{benchmarkClassName}|}}>()" : $"(typeof({{|#0:{benchmarkClassName}|}}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                const string benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           public abstract class {{benchmarkClassName}}
                                                                           {
                                                                               [Benchmark]
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                         
                                                                               }
                                                                           }
                                                                           """;
                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddDefaultExpectedDiagnostic(benchmarkClassName);

                await RunAsync();
            }
        }

        public class GenericTypeArgumentClassMustBeAnnotatedWithAGenericTypeArgumentsAttribute : AnalyzerTestFixture<RunAnalyzer>
        {
            public GenericTypeArgumentClassMustBeAnnotatedWithAGenericTypeArgumentsAttribute() : base(RunAnalyzer.GenericTypeArgumentClassMustBeAnnotatedWithAGenericTypeArgumentsAttributeRule) { }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_generic_class_annotated_with_at_least_one_generictypearguments_attribute_should_not_trigger_diagnostic([CombinatorialRange(1, 2)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                                     [CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                     [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                                     [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run(typeof({{benchmarkClassName}}<{{new string(',', typeParametersListLength - 1)}}>));
                                                        }
                                                    }
                                                    """;

                var genericTypeArguments = string.Join(", ", GenericTypeArguments.Select(ta => $"typeof({ta})").Take(typeParametersListLength));
                var genericTypeArgumentsAttributeUsages = string.Join("\n", Enumerable.Repeat($"[GenericTypeArguments({genericTypeArguments})]", genericTypeArgumentsAttributeUsageCount));
                var typeParameters = string.Join(", ", TypeParameters.Take(typeParametersListLength));

                var benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                  using BenchmarkDotNet.Attributes;
                                                                  
                                                                  {{genericTypeArgumentsAttributeUsages}}
                                                                  public class BenchmarkClass<{{typeParameters}}> : BenchmarkClassAncestor1<{{typeParameters}}>
                                                                  {
                                                                      {{benchmarkAttributeUsage}}
                                                                      public void BenchmarkMethod()
                                                                      {
                                                                  
                                                                      }
                                                                  }
                                                                  """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1<{{typeParameters}}> : BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                               
                                                                           }
                                                                           """;
                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;
                                                                           
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                           
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                                                                                                 
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_nongeneric_class_that_inherits_from_a_generic_class_not_annotated_with_a_generictypearguments_attribute_should_not_trigger_diagnostic(bool isGenericInvocation,
                                                                                                                                                                                    [CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                                                                    [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                                                                    [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var invocationExpression = isGenericInvocation ? $"<{benchmarkClassName}>()" : $"(typeof({benchmarkClassName}))";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run{{invocationExpression}};
                                                        }
                                                    }
                                                    """;

                var benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                  using BenchmarkDotNet.Attributes;
                                                                  
                                                                  public class {{benchmarkClassName}} : BenchmarkClassAncestor<{{string.Join(", ", GenericTypeArguments.Take(typeParametersListLength))}}>
                                                                  {
                                                                      
                                                                  }
                                                                  """;
                var benchmarkClassAncestorDocument = /* lang=c#-test */ $$"""
                                                                          using BenchmarkDotNet.Attributes;

                                                                          public {{abstractModifier}}class BenchmarkClassAncestor<{{string.Join(", ", TypeParameters.Take(typeParametersListLength))}}>
                                                                          {
                                                                          
                                                                              {{benchmarkAttributeUsage}}
                                                                              public void BenchmarkMethod()
                                                                              {
                                                                                                                                                
                                                                              }
                                                                          }
                                                                          """;

                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestorDocument);

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task A_generic_class_not_referenced_in_run_method_should_not_trigger_diagnostic([CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                         [CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                         [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                var typeParameters = string.Join(", ", TypeParameters.Take(typeParametersListLength));

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;

                                                    public class BenchmarkClass<{{string.Join(", ", TypeParameters.Take(typeParametersListLength))}}> : BenchmarkClassAncestor1<{{typeParameters}}>
                                                    {
                                                    }
                                                    """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1<{{typeParameters}}> : BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2<{{typeParameters}}> : BenchmarkClassAncestor3
                                                                           {
                                                                           }
                                                                           """;

                var benchmarkClassAncestor3Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           public {{abstractModifier}}class BenchmarkClassAncestor3
                                                                           {
                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
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

                await RunAsync();
            }

            [Theory, CombinatorialData]
            public async Task Invoking_with_a_generic_class_not_annotated_with_a_generictypearguments_attribute_should_trigger_diagnostic([CombinatorialRange(0, 3)] int genericTypeArgumentsAttributeUsageCount,
                                                                                                                                          [CombinatorialMemberData(nameof(TypeParametersListLengthEnumerableLocal))] int typeParametersListLength,
                                                                                                                                          [CombinatorialMemberData(nameof(ClassAbstractModifiersEnumerableLocal))] string abstractModifier,
                                                                                                                                          [CombinatorialMemberData(nameof(BenchmarkAttributeUsagesEnumerableLocal))] string benchmarkAttributeUsage)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var unboundGenericTypeParameterList = new string(',', typeParametersListLength - 1);
                var typeParameters = string.Join(", ", TypeParameters.Take(typeParametersListLength));
                var genericTypeArguments = string.Join(", ", GenericTypeArguments.Select(ta => $"typeof({ta})").Take(typeParametersListLength));
                var genericTypeArgumentsAttributeUsages = string.Join("\n", Enumerable.Repeat($"[GenericTypeArguments({genericTypeArguments})]", genericTypeArgumentsAttributeUsageCount));

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run(typeof({|#0:{{benchmarkClassName}}<{{unboundGenericTypeParameterList}}>|}));
                                                        }
                                                    }
                                                    """;

                var benchmarkClassDocument = /* lang=c#-test */ $$"""
                                                                  using BenchmarkDotNet.Attributes;
                                                                  
                                                                  public class {{benchmarkClassName}}<{{typeParameters}}> : BenchmarkClassAncestor1<{{typeParameters}}>
                                                                  {
                                                                      {{benchmarkAttributeUsage}}
                                                                      public void BenchmarkMethod()
                                                                      {
                                                                  
                                                                      }
                                                                  }
                                                                  """;

                var benchmarkClassAncestor1Document = /* lang=c#-test */ $$"""
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor1<{{typeParameters}}> : BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {
                                                                               
                                                                           }
                                                                           """;

                var benchmarkClassAncestor2Document = /* lang=c#-test */ $$"""
                                                                           using BenchmarkDotNet.Attributes;

                                                                           {{genericTypeArgumentsAttributeUsages}}
                                                                           public {{abstractModifier}}class BenchmarkClassAncestor2<{{typeParameters}}>
                                                                           {

                                                                               {{benchmarkAttributeUsage}}
                                                                               public void BenchmarkMethod()
                                                                               {
                                                                                                                                                 
                                                                               }
                                                                           }
                                                                           """;

                TestCode = testCode;
                AddSource(benchmarkClassDocument);
                AddSource(benchmarkClassAncestor1Document);
                AddSource(benchmarkClassAncestor2Document);

                AddDefaultExpectedDiagnostic($"{benchmarkClassName}<{unboundGenericTypeParameterList}>");

                await RunAsync();
            }

            public static IEnumerable<string> ClassAbstractModifiersEnumerableLocal => ClassAbstractModifiersEnumerable;

            public static IEnumerable<string> BenchmarkAttributeUsagesEnumerableLocal => BenchmarkAttributeUsagesEnumerable;

            public static IEnumerable<int> TypeParametersListLengthEnumerableLocal => TypeParametersListLengthEnumerable;
        }

        public static IEnumerable<string> ClassAbstractModifiersEnumerable => [ "", "abstract " ];

        public static IEnumerable<string> BenchmarkAttributeUsagesEnumerable => [ "", "[Benchmark]" ];

        public static IEnumerable<int> TypeParametersListLengthEnumerable => Enumerable.Range(1, TypeParameters.Count);

        private static ReadOnlyCollection<string> TypeParameters => Enumerable.Range(1, 3)
                                                                              .Select(i => $"TParameter{i}")
                                                                              .ToList()
                                                                              .AsReadOnly();

        private static ReadOnlyCollection<string> GenericTypeArguments => new List<string> { "int", "string", "bool" }.AsReadOnly();
    }
}
