using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public abstract class CodeFixTestFixture<TAnalyzer, TCodeFix> : TestFixtureBase
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    private readonly CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier> _codeFixTest;
    private readonly DiagnosticDescriptor? _ruleUnderTest;

    protected CodeFixTestFixture(DiagnosticDescriptor diagnosticDescriptor)
    {
        _codeFixTest = new InternalCodeFixTest
        {
            ReferenceAssemblies = GetReferenceAssemblies(),
        };
        AddAdditionalReferences(_codeFixTest.TestState);

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

    /// <summary>Set to <see cref="OutputKind.ConsoleApplication"/> to compile the test code as top-level statements.</summary>
    protected OutputKind OutputKind
    {
        set
        {
            _codeFixTest.TestState.OutputKind = value;
            _codeFixTest.FixedState.OutputKind = value;
        }
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
