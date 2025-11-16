using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GeneralArgumentAttributesAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor MethodWithoutAttributeMustHaveNoParametersRule = new(
        DiagnosticIds.Attributes_GeneralArgumentAttributes_MethodWithoutAttributeMustHaveNoParameters,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralArgumentAttributes_MethodWithoutAttributeMustHaveNoParameters_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralArgumentAttributes_MethodWithoutAttributeMustHaveNoParameters_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: BenchmarkDotNetAnalyzerResources.Attributes_GeneralArgumentAttributes_MethodWithoutAttributeMustHaveNoParameters_Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        MethodWithoutAttributeMustHaveNoParametersRule,
    }.ToImmutableArray();

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.EnableConcurrentExecution();
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        analysisContext.RegisterCompilationStartAction(ctx =>
        {
            // Only run if BenchmarkDotNet.Annotations is referenced
            var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(ctx.Compilation);
            if (benchmarkAttributeTypeSymbol == null)
            {
                return;
            }

            ctx.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        var argumentsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsAttribute");
        var argumentsSourceAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsSourceAttribute");

        if (argumentsAttributeTypeSymbol == null || argumentsSourceAttributeTypeSymbol == null)
        {
            return;
        }

        var hasBenchmarkAttribute = AnalyzerHelper.AttributeListsContainAttribute(AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation), methodDeclarationSyntax.AttributeLists, context.SemanticModel);
        var hasArgumentsSourceAttribute = AnalyzerHelper.AttributeListsContainAttribute(argumentsSourceAttributeTypeSymbol, methodDeclarationSyntax.AttributeLists, context.SemanticModel);

        var argumentsAttributes = AnalyzerHelper.GetAttributes(argumentsAttributeTypeSymbol, methodDeclarationSyntax.AttributeLists, context.SemanticModel);
        if (argumentsAttributes.Length == 0)
        {
            if (hasBenchmarkAttribute && !hasArgumentsSourceAttribute && methodDeclarationSyntax.ParameterList.Parameters.Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MethodWithoutAttributeMustHaveNoParametersRule,
                    Location.Create(context.Node.SyntaxTree, methodDeclarationSyntax.ParameterList.Parameters.Span), methodDeclarationSyntax.Identifier.ToString())
                );
            }
        }
    }
}