using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public abstract class AnalyzerTestFixture<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    private readonly CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> _analyzerTest;
    private readonly DiagnosticDescriptor? _ruleUnderTest;

    private AnalyzerTestFixture(bool assertUniqueSupportedDiagnostics)
    {
        _analyzerTest = new InternalAnalyzerTest
        {
#if NET8_0_OR_GREATER
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

        if (assertUniqueSupportedDiagnostics)
        {
            AssertUniqueSupportedDiagnostics();
        }
    }

    protected AnalyzerTestFixture() : this(true) { }

    protected AnalyzerTestFixture(DiagnosticDescriptor diagnosticDescriptor) : this(false)
    {
        var analyzer = AssertUniqueSupportedDiagnostics();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (diagnosticDescriptor == null)
        {
            Assert.Fail("Diagnostic under test cannot be null when using this constructor");
        }

        AssertDiagnosticUnderTestIsSupportedByAnalyzer();
        DisableAllSupportedDiagnosticsExceptDiagnosticUnderTest();

        _ruleUnderTest = diagnosticDescriptor;

        void AssertDiagnosticUnderTestIsSupportedByAnalyzer()
        {
            if (!analyzer.SupportedDiagnostics.Any(dd => dd.Id == diagnosticDescriptor.Id))
            {
                Assert.Fail($"Diagnostic descriptor with ID {diagnosticDescriptor.Id} is not supported by the analyzer {typeof(TAnalyzer).Name}");
            }
        }

        void DisableAllSupportedDiagnosticsExceptDiagnosticUnderTest()
        {
            _analyzerTest.DisabledDiagnostics.Clear();
            _analyzerTest.DisabledDiagnostics.AddRange(
                analyzer.SupportedDiagnostics
                    .Select(dd => dd.Id)
                    .Except([diagnosticDescriptor.Id])
            );
        }
    }

    private static TAnalyzer AssertUniqueSupportedDiagnostics()
    {
        var allSupportedDiagnostics = new Dictionary<string, int>();

        var analyzer = new TAnalyzer();
        foreach (var supportedDiagnostic in analyzer.SupportedDiagnostics)
        {
            if (allSupportedDiagnostics.TryGetValue(supportedDiagnostic.Id, out int value))
            {
                allSupportedDiagnostics[supportedDiagnostic.Id] = ++value;
            }
            else
            {
                allSupportedDiagnostics[supportedDiagnostic.Id] = 1;
            }
        }

        var duplicateSupportedDiagnostics = allSupportedDiagnostics
            .Where(kvp => kvp.Value > 1)
            .OrderBy(kvp => kvp.Key)
            .ToArray();

        if (duplicateSupportedDiagnostics.Length > 0)
        {
            Assert.Fail($"The analyzer {typeof(TAnalyzer).FullName} contains duplicate supported diagnostics:{Environment.NewLine}{Environment.NewLine}{string.Join(", ", duplicateSupportedDiagnostics.Select(kvp => $"❌ {kvp.Key} (x{kvp.Value})"))}{Environment.NewLine}");
        }

        return analyzer;
    }

    protected string TestCode
    {
        set => _analyzerTest.TestCode = value;
    }

    protected void AddSource(string filename, string content)
        => _analyzerTest.TestState.Sources.Add((filename, content));

    protected void AddSource(string content)
        => _analyzerTest.TestState.Sources.Add(content);

    protected void AddDefaultExpectedDiagnostic()
        => AddExpectedDiagnostic();

    protected void AddDefaultExpectedDiagnostic(params object[] arguments)
        => AddExpectedDiagnostic(arguments);

    protected void AddDefaultExpectedDiagnostic(DiagnosticSeverity effectiveDiagnosticSeverity)
        => AddExpectedDiagnostic(effectiveDiagnosticSeverity: effectiveDiagnosticSeverity);

    protected void AddDefaultExpectedDiagnostic(DiagnosticSeverity effectiveDiagnosticSeverity, params object[] arguments)
        => AddExpectedDiagnostic(arguments, effectiveDiagnosticSeverity: effectiveDiagnosticSeverity);

    protected void AddExpectedDiagnostic(int markupKey)
        => AddExpectedDiagnostic(null, markupKey);

    protected void AddExpectedDiagnostic(int markupKey, DiagnosticSeverity effectiveDiagnosticSeverity)
        => AddExpectedDiagnostic(null, markupKey, effectiveDiagnosticSeverity);

    protected void AddExpectedDiagnostic(int markupKey, params object[] arguments)
        => AddExpectedDiagnostic(arguments, markupKey);

    protected void AddExpectedDiagnostic(int markupKey, DiagnosticSeverity effectiveDiagnosticSeverity, params object[] arguments)
        => AddExpectedDiagnostic(arguments, markupKey, effectiveDiagnosticSeverity);

    private void AddExpectedDiagnostic(object[]? arguments = null, int markupKey = 0, DiagnosticSeverity? effectiveDiagnosticSeverity = null)
    {
        if (_ruleUnderTest == null)
        {
            throw new InvalidOperationException("Failed to add expected diagnostic: no diagnostic rule specified for this fixture");
        }

        var diagnosticResult = new DiagnosticResult(_ruleUnderTest)
            .WithLocation(markupKey)
            .WithMessageFormat(_ruleUnderTest.MessageFormat);

        if (arguments != null)
        {
            diagnosticResult = diagnosticResult.WithArguments(arguments);
        }

        if (effectiveDiagnosticSeverity.HasValue)
        {
            diagnosticResult = diagnosticResult.WithSeverity(effectiveDiagnosticSeverity.Value);
        }

        _analyzerTest.ExpectedDiagnostics.Add(diagnosticResult);
    }

    protected void DisableCompilerDiagnostics()
        => _analyzerTest.CompilerDiagnostics = CompilerDiagnostics.None;

    protected Task RunAsync()
        => _analyzerTest.RunAsync(CancellationToken.None);

    protected void ReferenceDummyAttribute()
        => _analyzerTest.TestState.Sources.Add("""
            using System;

            public class DummyAttribute : Attribute
            {
                                                
            }
            """
        );

    protected void ReferenceDummyEnum()
        => _analyzerTest.TestState.Sources.Add("""
            public enum DummyEnum
            {
                Value1,
                Value2,
                Value3
            }
            """
        );

    protected void ReferenceDummyEnumInDifferentNamespace()
        => _analyzerTest.TestState.Sources.Add("""
            namespace DifferentNamespace;
            
            public enum DummyEnumInDifferentNamespace
            {
                Value1,
                Value2,
                Value3
            }
            """
        );

    protected void ReferenceDummyEnumWithFlagsAttribute()
        => _analyzerTest.TestState.Sources.Add("""
            using System;

            [Flags]
            public enum DummyEnumWithFlagsAttribute
            {
                Value1,
                Value2,
                Value3
            }
            """
        );

    protected void ReferenceConstants(string type, string value)
        => _analyzerTest.TestState.Sources.Add($$"""
            using System;

            public static class Constants
            {
                public const {{type}} Value = {{value}};
            }
            """
        );

    protected void ReferenceConstants(params (string Type, string Value)[] constants)
        => _analyzerTest.TestState.Sources.Add($$"""
            using System;

            public static class Constants
            {
                {{string.Join("\n   ", constants.Select((c, i) => $"public const {c.Type} Value{i + 1} = {c.Value};"))}}
            }
            """
        );

    protected void SetParseOptions(LanguageVersion languageVersion, bool interceptorsNamespaces = false)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);

        if (interceptorsNamespaces)
        {
            parseOptions = parseOptions.WithFeatures([ new KeyValuePair<string, string>(AnalyzerHelper.InterceptorsNamespaces, "") ]);
        }

        _analyzerTest.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(projectId, parseOptions));
    }

    private sealed class InternalAnalyzerTest : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    {
        protected override string DefaultTestProjectName => "BenchmarksAssemblyUnderAnalysis";
    }
}