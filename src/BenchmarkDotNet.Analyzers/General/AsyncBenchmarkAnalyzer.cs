using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Analyzers.General;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsyncBenchmarkAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor AsyncBenchmarkShouldHaveCancellationTokenRule = new(
        DiagnosticIds.General_AsyncBenchmark_ShouldHaveCancellationToken,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AsyncBenchmark_ShouldHaveCancellationToken_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AsyncBenchmark_ShouldHaveCancellationToken_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_AsyncBenchmark_ShouldHaveCancellationToken_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        AsyncBenchmarkShouldHaveCancellationTokenRule,
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

            ctx.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        });
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
        if (benchmarkAttributeTypeSymbol == null)
        {
            return;
        }

        var benchmarkCancellationAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkCancellationAttribute");
        if (benchmarkCancellationAttributeTypeSymbol == null)
        {
            return;
        }

        // Check if the class has any async benchmark methods
        MethodDeclarationSyntax? firstAsyncBenchmarkMethod = null;

        foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
        {
            if (memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                // Check if method has [Benchmark] attribute
                var hasBenchmarkAttribute = false;
                foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
                {
                    foreach (var attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
                        if (attributeSyntaxTypeSymbol != null &&
                            SymbolEqualityComparer.Default.Equals(attributeSyntaxTypeSymbol, benchmarkAttributeTypeSymbol))
                        {
                            hasBenchmarkAttribute = true;
                            break;
                        }
                    }
                    if (hasBenchmarkAttribute)
                        break;
                }

                // Check if method has async modifier
                if (hasBenchmarkAttribute && methodDeclarationSyntax.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    firstAsyncBenchmarkMethod = methodDeclarationSyntax;
                    break;
                }
            }
        }

        // If no async benchmark methods, nothing to check
        if (firstAsyncBenchmarkMethod == null)
        {
            return;
        }

        // Check if class has a [BenchmarkCancellation] member
        var hasCancellationTokenMember = false;

        foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
        {
            AttributeListSyntax? attributeList = null;

            if (memberDeclarationSyntax is FieldDeclarationSyntax fieldDeclarationSyntax)
            {
                attributeList = fieldDeclarationSyntax.AttributeLists.FirstOrDefault();
            }
            else if (memberDeclarationSyntax is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                attributeList = propertyDeclarationSyntax.AttributeLists.FirstOrDefault();
            }

            if (attributeList != null)
            {
                foreach (var attributeSyntax in attributeList.Attributes)
                {
                    var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
                    if (attributeSyntaxTypeSymbol != null &&
                        SymbolEqualityComparer.Default.Equals(attributeSyntaxTypeSymbol, benchmarkCancellationAttributeTypeSymbol))
                    {
                        hasCancellationTokenMember = true;
                        break;
                    }
                }
            }

            if (hasCancellationTokenMember)
                break;
        }

        // If class has async benchmarks but no [BenchmarkCancellation] member, suggest adding one
        if (!hasCancellationTokenMember)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AsyncBenchmarkShouldHaveCancellationTokenRule,
                firstAsyncBenchmarkMethod.Identifier.GetLocation(),
                classDeclarationSyntax.Identifier.ToString()));
        }
    }
}
