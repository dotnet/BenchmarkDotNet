using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.Attributes;

/// <summary>
/// Static counterpart to <c>SetupCleanupValidator.ValidateReturnType</c>: a method annotated with
/// <c>[GlobalSetup]</c>, <c>[GlobalCleanup]</c>, <c>[IterationSetup]</c>, or <c>[IterationCleanup]</c>
/// must not return an async enumerable. BenchmarkDotNet awaits awaitable returns from those methods but
/// does not enumerate async enumerables, so the iterator body would silently never run.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupCleanupAsyncEnumerableAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor MustNotReturnAsyncEnumerableRule = new(
        DiagnosticIds.Attributes_SetupCleanup_MustNotReturnAsyncEnumerable,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_SetupCleanup_MustNotReturnAsyncEnumerable_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_SetupCleanup_MustNotReturnAsyncEnumerable_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_SetupCleanup_MustNotReturnAsyncEnumerable_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        MustNotReturnAsyncEnumerableRule,
    }.ToImmutableArray();

    private static readonly string[] SetupCleanupAttributeMetadataNames =
    [
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
            // Only run if BenchmarkDotNet.Annotations is referenced.
            if (AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(ctx.Compilation) == null)
            {
                return;
            }

            var attributeSymbols = ImmutableArray.CreateBuilder<INamedTypeSymbol>(SetupCleanupAttributeMetadataNames.Length);
            foreach (var metadataName in SetupCleanupAttributeMetadataNames)
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

        // Find which (if any) setup/cleanup attribute is applied.
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

        // Mirrors ReflectionExtensions.IsAsyncEnumerable: exact IAsyncEnumerable<T> short-circuit, then
        // public-instance GetAsyncEnumerator pattern, then interface fallback. The runtime validator
        // also explicitly skips awaitable types — if the return type happens to be both awaitable AND
        // an async enumerable, BenchmarkDotNet awaits it instead of rejecting it (BDN1701 covers that
        // ambiguity separately).
        var returnType = methodSymbol.ReturnType;
        if (!AsyncTypeShapes.IsAsyncEnumerable(returnType, captured.AsyncEnumerableInterfaceSymbol))
        {
            return;
        }

        if (AsyncTypeShapes.IsAwaitable(returnType))
        {
            return;
        }

        var attributeShortName = matchedAttribute.Name.EndsWith("Attribute")
            ? matchedAttribute.Name.Substring(0, matchedAttribute.Name.Length - "Attribute".Length)
            : matchedAttribute.Name;

        context.ReportDiagnostic(Diagnostic.Create(
            MustNotReturnAsyncEnumerableRule,
            methodDeclarationSyntax.ReturnType.GetLocation(),
            attributeShortName,
            methodSymbol.Name));
    }

}
