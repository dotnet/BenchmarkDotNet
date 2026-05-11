using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.General;

/// <summary>
/// Flags methods whose return type satisfies BOTH the awaitable pattern (public parameterless
/// <c>GetAwaiter</c>) AND the async-enumerable pattern (public <c>GetAsyncEnumerator</c> with the
/// required shape, or <c>IAsyncEnumerable&lt;T&gt;</c>). BenchmarkDotNet's runtime resolution prefers
/// the awaitable path for these — the enumerator's body would never run — so author intent is
/// almost certainly miswired. Covers <c>[Benchmark]</c>, <c>[GlobalSetup]</c>, <c>[GlobalCleanup]</c>,
/// <c>[IterationSetup]</c>, and <c>[IterationCleanup]</c> methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AwaitableAsyncEnumerableAmbiguityAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor AmbiguousReturnTypeRule = new(
        DiagnosticIds.General_AwaitableAsyncEnumerable_AmbiguousReturnType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AwaitableAsyncEnumerable_AmbiguousReturnType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AwaitableAsyncEnumerable_AmbiguousReturnType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AwaitableAsyncEnumerable_AmbiguousReturnType_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        AmbiguousReturnTypeRule,
    }.ToImmutableArray();

    private static readonly string[] WatchedAttributeMetadataNames =
    [
        "BenchmarkDotNet.Attributes.BenchmarkAttribute",
        "BenchmarkDotNet.Attributes.GlobalSetupAttribute",
        "BenchmarkDotNet.Attributes.GlobalCleanupAttribute",
        "BenchmarkDotNet.Attributes.IterationSetupAttribute",
        "BenchmarkDotNet.Attributes.IterationCleanupAttribute",
    ];

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.EnableConcurrentExecution();
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        analysisContext.RegisterCompilationStartAction(ctx =>
        {
            if (AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(ctx.Compilation) == null)
            {
                return;
            }

            var attributeSymbols = ImmutableArray.CreateBuilder<INamedTypeSymbol>(WatchedAttributeMetadataNames.Length);
            foreach (var metadataName in WatchedAttributeMetadataNames)
            {
                var symbol = ctx.Compilation.GetTypeByMetadataName(metadataName);
                if (symbol != null)
                {
                    attributeSymbols.Add(symbol);
                }
            }

            if (attributeSymbols.Count == 0)
            {
                return;
            }

            var asyncEnumerableInterfaceSymbol = ctx.Compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
            var captured = (attributeSymbols.ToImmutable(), asyncEnumerableInterfaceSymbol);
            ctx.RegisterSyntaxNodeAction(c => AnalyzeMethod(c, captured), SyntaxKind.MethodDeclaration);
        });
    }

    private static void AnalyzeMethod(
        SyntaxNodeAnalysisContext context,
        (ImmutableArray<INamedTypeSymbol> AttributeSymbols, INamedTypeSymbol? AsyncEnumerableInterfaceSymbol) captured)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol methodSymbol)
        {
            return;
        }

        INamedTypeSymbol? matchedAttribute = null;
        foreach (var attributeData in methodSymbol.GetAttributes())
        {
            foreach (var candidate in captured.AttributeSymbols)
            {
                if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, candidate))
                {
                    matchedAttribute = candidate;
                    break;
                }
            }
            if (matchedAttribute != null) break;
        }

        if (matchedAttribute == null)
        {
            return;
        }

        var returnType = methodSymbol.ReturnType;
        if (!AsyncTypeShapes.IsAwaitable(returnType))
        {
            return;
        }

        if (!AsyncTypeShapes.IsAsyncEnumerable(returnType, captured.AsyncEnumerableInterfaceSymbol))
        {
            return;
        }

        var attributeShortName = matchedAttribute.Name.EndsWith("Attribute")
            ? matchedAttribute.Name.Substring(0, matchedAttribute.Name.Length - "Attribute".Length)
            : matchedAttribute.Name;

        context.ReportDiagnostic(Diagnostic.Create(
            AmbiguousReturnTypeRule,
            methodDeclarationSyntax.ReturnType.GetLocation(),
            attributeShortName,
            methodSymbol.Name,
            returnType.ToDisplayString()));
    }
}
