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

        internal static readonly DiagnosticDescriptor TypeArgumentClassMustBePublicRule = new DiagnosticDescriptor(DiagnosticIds.BenchmarkRunner_Run_TypeArgumentClassMustBePublic,
                                                                                                                   AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBePublic_Title)),
                                                                                                                   AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBePublic_MessageFormat)),
                                                                                                                   "Usage",
                                                                                                                   DiagnosticSeverity.Error,
                                                                                                                   isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor TypeArgumentClassMustBeNonAbstractRule = new DiagnosticDescriptor(DiagnosticIds.BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract,
                                                                                                                        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract_Title)),
                                                                                                                        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract_MessageFormat)),
                                                                                                                        "Usage",
                                                                                                                        DiagnosticSeverity.Error,
                                                                                                                        isEnabledByDefault: true,
                                                                                                                        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeNonAbstract_Description)));

        internal static readonly DiagnosticDescriptor TypeArgumentClassMustBeUnsealedRule = new DiagnosticDescriptor(DiagnosticIds.BenchmarkRunner_Run_TypeArgumentClassMustBeUnsealed,
                                                                                                                     AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeUnsealed_Title)),
                                                                                                                     AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeUnsealed_MessageFormat)),
                                                                                                                     "Usage",
                                                                                                                     DiagnosticSeverity.Error,
                                                                                                                     isEnabledByDefault: true,
                                                                                                                     description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.BenchmarkRunner_Run_TypeArgumentClassMustBeUnsealed_Description)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            TypeArgumentClassMissingBenchmarkMethodsRule,
            TypeArgumentClassMustBePublicRule,
            TypeArgumentClassMustBeNonAbstractRule,
            TypeArgumentClassMustBeUnsealedRule
        ];

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
            if (context.Node is not InvocationExpressionSyntax invocationExpression)
            {
                return;
            }

            if (invocationExpression.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
            {
                return;
            }

            if (memberAccessExpression.Expression is not IdentifierNameSyntax identifierNameSyntax)
            {
                return;
            }

            var classMemberAccessTypeSymbol = context.SemanticModel.GetTypeInfo(identifierNameSyntax).Type;
            if (    classMemberAccessTypeSymbol is null
                ||  classMemberAccessTypeSymbol.TypeKind == TypeKind.Error
                || !classMemberAccessTypeSymbol.Equals(context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Running.BenchmarkRunner"), SymbolEqualityComparer.Default))
            {
                return;
            }

            if (memberAccessExpression.Name.Identifier.ValueText != "Run")
            {
                return;
            }

            INamedTypeSymbol? benchmarkClassTypeSymbol;
            Location? diagnosticLocation;

            if (memberAccessExpression.Name is GenericNameSyntax genericMethod)
            {
                if (genericMethod.TypeArgumentList.Arguments.Count != 1)
                {
                    return;
                }

                diagnosticLocation = Location.Create(context.FilterTree, genericMethod.TypeArgumentList.Arguments.Span);
                benchmarkClassTypeSymbol = context.SemanticModel.GetTypeInfo(genericMethod.TypeArgumentList.Arguments[0]).Type as INamedTypeSymbol;
            }
            else
            {
                if (invocationExpression.ArgumentList.Arguments.Count == 0)
                {
                    return;
                }

                // TODO: Support analyzing an array of typeof() expressions
                if (invocationExpression.ArgumentList.Arguments[0].Expression is not TypeOfExpressionSyntax typeOfExpression)
                {
                    return;
                }

                diagnosticLocation = typeOfExpression.Type.GetLocation();
                benchmarkClassTypeSymbol = context.SemanticModel.GetTypeInfo(typeOfExpression.Type).Type as INamedTypeSymbol;

            }

            if (benchmarkClassTypeSymbol == null || benchmarkClassTypeSymbol.TypeKind == TypeKind.Error || (benchmarkClassTypeSymbol.IsGenericType && !benchmarkClassTypeSymbol.IsUnboundGenericType))
            {
                return;
            }

            var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
            if (benchmarkAttributeTypeSymbol == null)
            {
                ReportDiagnostic(TypeArgumentClassMissingBenchmarkMethodsRule);

                return;
            }

            if (!HasBenchmarkAttribute())
            {
                ReportDiagnostic(TypeArgumentClassMissingBenchmarkMethodsRule);
            }

            if (benchmarkClassTypeSymbol.DeclaredAccessibility != Accessibility.Public)
            {
                ReportDiagnostic(TypeArgumentClassMustBePublicRule);
            }

            if (benchmarkClassTypeSymbol.IsAbstract)
            {
                ReportDiagnostic(TypeArgumentClassMustBeNonAbstractRule);
            }

            if (benchmarkClassTypeSymbol.IsSealed)
            {
                ReportDiagnostic(TypeArgumentClassMustBeUnsealedRule);
            }

            return;

            bool HasBenchmarkAttribute()
            {
                var baseType = benchmarkClassTypeSymbol;

                while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                {
                    foreach (var member in baseType.GetMembers())
                    {
                        if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary })
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

                    baseType = baseType.OriginalDefinition.BaseType;
                }

                return false;
            }

            void ReportDiagnostic(DiagnosticDescriptor diagnosticDescriptor)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, diagnosticLocation, AnalyzerHelper.NormalizeTypeName(benchmarkClassTypeSymbol)));
            }
        }
    }
}
