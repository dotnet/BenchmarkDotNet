using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public abstract class CodeFixTestFixture<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private readonly CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> _codeFixTest;
    private readonly DiagnosticDescriptor? _ruleUnderTest;

    protected CodeFixTestFixture(DiagnosticDescriptor diagnosticDescriptor)
    {
        _codeFixTest = new InternalCodeFixTest
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
                    "System.Memory.dll"
#endif
                }
            }
        };

        var analyzer = new TAnalyzer();
        if (diagnosticDescriptor == null)
        {
            Assert.Fail("Diagnostic under test cannot be null");
        }

        if (!analyzer.SupportedDiagnostics.Any(dd => dd.Id == diagnosticDescriptor.Id))
        {
            Assert.Fail($"Diagnostic descriptor with ID {diagnosticDescriptor.Id} is not supported by the analyzer {typeof(TAnalyzer).Name}");
        }

        _codeFixTest.DisabledDiagnostics.Clear();
        _codeFixTest.DisabledDiagnostics.AddRange(
            analyzer.SupportedDiagnostics
                .Select(dd => dd.Id)
                .Except([diagnosticDescriptor.Id])
        );

        _ruleUnderTest = diagnosticDescriptor;
    }

    protected string TestCode
    {
        set => _codeFixTest.TestCode = value;
    }

    protected string FixedCode
    {
        set => _codeFixTest.FixedCode = value;
    }

    protected void AddExpectedDiagnostic(int markupKey, params object[] arguments)
    {
        if (_ruleUnderTest == null)
        {
            throw new InvalidOperationException("Failed to add expected diagnostic: no diagnostic rule specified for this fixture");
        }

        var diagnosticResult = new DiagnosticResult(_ruleUnderTest)
            .WithLocation(markupKey)
            .WithMessageFormat(_ruleUnderTest.MessageFormat);

        if (arguments != null && arguments.Length > 0)
        {
            diagnosticResult = diagnosticResult.WithArguments(arguments);
        }

        _codeFixTest.ExpectedDiagnostics.Add(diagnosticResult);
    }

    protected Task RunAsync()
        => _codeFixTest.RunAsync(CancellationToken.None);

    private sealed class InternalCodeFixTest : CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        protected override string DefaultTestProjectName => "BenchmarksAssemblyUnderAnalysis";
    }
}
