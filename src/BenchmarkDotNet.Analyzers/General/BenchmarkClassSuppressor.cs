using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.General;

/// <summary>
/// Suppresses CA1515 for classes containing benchmark methods: BenchmarkDotNet requires benchmark classes to be
/// public, so making them internal is not an option.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BenchmarkClassSuppressor : DiagnosticSuppressor
{
    private const string SuppressedDiagnosticId = "CA1515";

    internal static readonly SuppressionDescriptor Rule = new(
        DiagnosticIds.General_BenchmarkClass_SuppressCA1515,
        SuppressedDiagnosticId,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_SuppressCA1515_Justification)));

    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [Rule];

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
        if (benchmarkAttributeTypeSymbol == null)
        {
            return;
        }

        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (diagnostic.Id != SuppressedDiagnosticId || diagnostic.Location.SourceTree is not { } sourceTree)
            {
                continue;
            }

            var root = sourceTree.GetRoot(context.CancellationToken);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                continue;
            }

            var semanticModel = context.GetSemanticModel(sourceTree);
            if (semanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is INamedTypeSymbol { TypeKind: TypeKind.Class } type
                && ContainsBenchmarkMethod(type, benchmarkAttributeTypeSymbol))
            {
                context.ReportSuppression(Suppression.Create(Rule, diagnostic));
            }
        }
    }

    // BenchmarkDotNet discovers benchmarks through reflection over the type and its base types, so an inherited
    // benchmark method makes a derived class a benchmark class as well.
    private static bool ContainsBenchmarkMethod(INamedTypeSymbol type, INamedTypeSymbol benchmarkAttributeTypeSymbol)
    {
        for (ITypeSymbol? current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary } method)
                {
                    continue;
                }

                foreach (var attribute in method.GetAttributes())
                {
                    if (AnalyzerHelper.IsOrDerivesFrom(attribute.AttributeClass, benchmarkAttributeTypeSymbol))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
