using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

/// <summary>
/// Tests a <see cref="DiagnosticSuppressor"/> by pairing it with an analyzer that reports the suppressed
/// diagnostic for every public type in the test code.
/// </summary>
public abstract class SuppressorTestFixture<TSuppressor>
    where TSuppressor : DiagnosticSuppressor, new()
{
    private readonly CSharpAnalyzerTest<TSuppressor, DefaultVerifier> _suppressorTest;
    private readonly DiagnosticDescriptor _suppressedDiagnosticRule;

    protected SuppressorTestFixture()
    {
        var suppressor = new TSuppressor();
        var suppression = Assert.Single(suppressor.SupportedSuppressions);

        _suppressedDiagnosticRule = new DiagnosticDescriptor(
            suppression.SuppressedDiagnosticId,
            "Public type",
            "Consider making public types internal",
            "Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        _suppressorTest = new InternalSuppressorTest(_suppressedDiagnosticRule)
        {
#if NET10_0_OR_GREATER
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
#elif NET8_0_OR_GREATER
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
#elif NET6_0_OR_GREATER
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
#else
            ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20,
#endif
            TestState =
            {
                AdditionalReferences =
                {
                    "BenchmarkDotNet.dll",
                    "BenchmarkDotNet.Annotations.dll",
#if !NET6_0_OR_GREATER
                    "System.Memory.dll",
                    "Microsoft.Bcl.AsyncInterfaces.dll"
#endif
                }
            }
        };
    }

    protected string TestCode
    {
        set => _suppressorTest.TestCode = value;
    }

    protected void AddSource(string filename, string content)
        => _suppressorTest.TestState.Sources.Add((filename, content));

    protected void AddSource(string content)
        => _suppressorTest.TestState.Sources.Add(content);

    protected void ClearAdditionalReferences()
        => _suppressorTest.TestState.AdditionalReferences.Clear();

    protected void AddExpectedDiagnostic(int markupKey, bool isSuppressed)
        => _suppressorTest.ExpectedDiagnostics.Add(
            new DiagnosticResult(_suppressedDiagnosticRule)
                .WithLocation(markupKey)
                .WithIsSuppressed(isSuppressed));

    protected Task RunAsync()
        => _suppressorTest.RunAsync(CancellationToken.None);

    private sealed class InternalSuppressorTest : CSharpAnalyzerTest<TSuppressor, DefaultVerifier>
    {
        private readonly DiagnosticDescriptor _rule;

        public InternalSuppressorTest(DiagnosticDescriptor rule)
        {
            _rule = rule;
        }

        protected override string DefaultTestProjectName => "BenchmarksAssemblyUnderAnalysis";

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            foreach (var analyzer in base.GetDiagnosticAnalyzers())
            {
                yield return analyzer;
            }

            yield return new PublicTypeAnalyzer(_rule);
        }
    }

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1038:Compiler extensions should be implemented in assemblies with compiler-provided references", Justification = "Test-only analyzer.")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1036:Specify analyzer banned API enforcement setting", Justification = "Test-only analyzer.")]
    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1041:Compiler extensions should be implemented in assemblies targeting netstandard2.0", Justification = "Test-only analyzer.")]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class PublicTypeAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule;

        public PublicTypeAnalyzer(DiagnosticDescriptor rule)
        {
            _rule = rule;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(
                symbolContext =>
                {
                    if (symbolContext.Symbol is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } type)
                    {
                        symbolContext.ReportDiagnostic(Diagnostic.Create(_rule, type.Locations[0]));
                    }
                },
                SymbolKind.NamedType);
        }
    }
}
