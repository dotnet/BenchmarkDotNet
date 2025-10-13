namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.BenchmarkRunner
{
    using Fixtures;

    using Analyzers.BenchmarkRunner;

    using Xunit;

    using System.Linq;
    using System.Threading.Tasks;

    public class RunAnalyzerTests
    {
        public class General : AnalyzerTestFixture<RunAnalyzer>
        {
            [Fact]
            public async Task Invoking_with_a_public_nonabstract_unsealed_type_argument_class_having_only_one_and_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;

                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{{classWithOneBenchmarkMethodName}}>();
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

            [Fact]
            public async Task Invoking_with_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_should_not_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;
                                                           
                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{{classWithOneBenchmarkMethodName}}>();
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
            public async Task Invoking_with_type_argument_class_having_at_least_one_public_method_annotated_with_the_benchmark_attribute_in_one_of_its_ancestor_classes_should_not_trigger_diagnostic([CombinatorialValues("", "abstract ")] string abstractModifier)
            {
                const string classWithOneBenchmarkMethodName = "TopLevelBenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;
                                                           
                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{{classWithOneBenchmarkMethodName}}>();
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

            [Fact]
            public async Task Invoking_with_type_argument_class_having_no_public_method_annotated_with_the_benchmark_attribute_should_trigger_diagnostic()
            {
                const string classWithOneBenchmarkMethodName = "ClassWithOneBenchmarkMethod";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;
                                                           
                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{|#0:{{classWithOneBenchmarkMethodName}}|}>();
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
        }

        public class TypeArgumentClassMustBePublic : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBePublic() : base(RunAnalyzer.TypeArgumentClassMustBePublicRule) { }

            [Fact]
            public async Task Invoking_with_a_nonpublic_class_with_multiple_inheritance_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic()
            {
                const string benchmarkClassName = "BenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Attributes;
                                                             using BenchmarkDotNet.Running;

                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{|#0:{{benchmarkClassName}}|}>();
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

            [Theory]
            [MemberData(nameof(NonPublicClassAccessModifiersExceptFileTheoryData))]
            public async Task Invoking_with_a_nonpublic_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic(string nonPublicClassAccessModifier)
            {
                const string benchmarkClassName = "BenchmarkClass";

                var testCode = /* lang=c#-test */ $$"""
                                                    using BenchmarkDotNet.Attributes;
                                                    using BenchmarkDotNet.Running;
                                                   
                                                    public class Program
                                                    {
                                                        public static void Main(string[] args) {
                                                            BenchmarkRunner.Run<{|#0:{{benchmarkClassName}}|}>();
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

            [Fact]
            public async Task Invoking_with_a_file_class_containing_at_least_one_method_annotated_with_benchmark_attribute_should_trigger_diagnostic()
            {
                const string benchmarkClassName = "BenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Attributes;
                                                             using BenchmarkDotNet.Running;

                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{|#0:{{benchmarkClassName}}|}>();
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

            public static TheoryData<string> NonPublicClassAccessModifiersExceptFileTheoryData => new(new NonPublicClassAccessModifiersTheoryData().Where<string>(m => m != "file "));
        }

        public class TypeArgumentClassMustBeNonAbstract : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBeNonAbstract() : base(RunAnalyzer.TypeArgumentClassMustBeNonAbstractRule) { }

            [Fact]
            public async Task Invoking_with_an_abstract_benchmark_class_should_trigger_diagnostic()
            {
                const string benchmarkClassName = "BenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;
                                                           
                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{|#0:{{benchmarkClassName}}|}>();
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

        public class TypeArgumentClassMustBeUnsealed : AnalyzerTestFixture<RunAnalyzer>
        {
            public TypeArgumentClassMustBeUnsealed() : base(RunAnalyzer.TypeArgumentClassMustBeUnsealedRule) { }

            [Fact]
            public async Task Invoking_with_a_sealed_benchmark_class_should_trigger_diagnostic()
            {
                const string benchmarkClassName = "BenchmarkClass";

                const string testCode = /* lang=c#-test */ $$"""
                                                             using BenchmarkDotNet.Running;
                                                           
                                                             public class Program
                                                             {
                                                                 public static void Main(string[] args) {
                                                                     BenchmarkRunner.Run<{|#0:{{benchmarkClassName}}|}>();
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
    }
}
