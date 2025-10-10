namespace BenchmarkDotNet.Analyzers.BenchmarkRunner
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RunAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor TypeArgumentClassMissingBenchmarkMethodsRule = new DiagnosticDescriptor(DiagnosticIds.BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods,
                                                                                                                              AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods_Title)),
                                                                                                                              AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods_MessageFormat)),
                                                                                                                              "Usage",
                                                                                                                              DiagnosticSeverity.Error,
                                                                                                                              isEnabledByDefault: true,
                                                                                                                              description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMissingBenchmarkMethods_Description)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(TypeArgumentClassMissingBenchmarkMethodsRule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterCompilationStartAction(ctx =>
            {
                // Only run if BenchmarkDotNet is referenced
                var benchmarkRunnerTypeSymbol = ctx.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Running.BenchmarkRunner");
                if (benchmarkRunnerTypeSymbol == null)
                {
                    return;
                }

                ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocationExpression))
            {
                return;
            }

            if (!(invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression))
            {
                return;
            }

            if (!(memberAccessExpression.Expression is IdentifierNameSyntax typeIdentifier))
            {
                return;
            }

            var classMemberAccessSymbol = context.SemanticModel.GetTypeInfo(typeIdentifier).Type;
            if (classMemberAccessSymbol is null || !classMemberAccessSymbol.Equals(context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Running.BenchmarkRunner"), SymbolEqualityComparer.Default))
            {
                return;
            }

            if (!(memberAccessExpression.Name is GenericNameSyntax genericMethod))
            {
                return;
            }

            if (genericMethod.Identifier.ValueText != "Run")
            {
                return;
            }

            if (genericMethod.TypeArgumentList.Arguments.Count != 1)
            {
                return;
            }

            var benchmarkClassTypeSymbol = context.SemanticModel.GetTypeInfo(genericMethod.TypeArgumentList.Arguments[0]).Type;
            if (benchmarkClassTypeSymbol == null || benchmarkClassTypeSymbol.TypeKind == TypeKind.Error)
            {
                return;
            }

            var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
            if (benchmarkAttributeTypeSymbol == null)
            {
                ReportDiagnostic();

                return;
            }

            if (!HasBenchmarkAttribute())
            {
                ReportDiagnostic();
            }

            return;

            bool HasBenchmarkAttribute()
            {
                foreach (var member in benchmarkClassTypeSymbol.GetMembers())
                {
                    if (member is IMethodSymbol)
                    {
                        foreach (var attributeData in member.GetAttributes())
                        {
                            if (attributeData.AttributeClass != null)
                            {
                                if (attributeData.AttributeClass.Equals(benchmarkAttributeTypeSymbol, SymbolEqualityComparer.Default))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }

            void ReportDiagnostic()
            {
                context.ReportDiagnostic(Diagnostic.Create(TypeArgumentClassMissingBenchmarkMethodsRule, Location.Create(context.FilterTree, genericMethod.TypeArgumentList.Arguments.Span), benchmarkClassTypeSymbol.ToString()));
            }
        }
    }
}
