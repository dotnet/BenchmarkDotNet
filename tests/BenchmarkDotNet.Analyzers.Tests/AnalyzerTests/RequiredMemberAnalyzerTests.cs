using BenchmarkDotNet.Analyzers;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using Microsoft.CodeAnalysis;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests;

public class RequiredMemberAnalyzerTests
{
    public class RequiredMemberCannotBeSet : AnalyzerTestFixture<RequiredMemberAnalyzer>
    {
        public RequiredMemberCannotBeSet() : base(RequiredMemberAnalyzer.RequiredMemberCannotBeSetRule) { }

        [Fact]
        public async Task A_required_member_without_a_settable_attribute_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public required string {|#0:Text|} { get; set; }

                    [GlobalSetup]
                    public void Setup() => Text = "";

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Text");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_field_without_a_settable_attribute_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    public required int {|#0:Value|};

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Value");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_params_member_does_not_report_error()
        {
            // [Params*] members are set in the runnable's object initializer, so `required` is satisfied.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public required int Value { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_benchmark_cancellation_member_does_not_report_error()
        {
            // An instance [BenchmarkCancellation] member is set in the cancellation-token initializer.
            var testCode = /* lang=c#-test */ """
                using System.Threading;
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [BenchmarkCancellation]
                    public required CancellationToken Token { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_member_inherited_from_a_non_benchmark_base_reports_error()
        {
            // The base has no benchmarks, but its required member is inherited by the benchmark type (and the runnable).
            // The diagnostic is reported at the benchmark class's `: BaseType` reference, not the base's declaration.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkBase
                {
                    public required string Text { get; set; }
                }

                public class BenchmarkClass : {|#0:BenchmarkBase|}
                {
                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Text");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_member_on_a_benchmark_base_is_reported_once_at_its_declaration()
        {
            // Both types are benchmark types (the derived inherits [Benchmark]); the required member is flagged only
            // once, at its declaration on the base - not again at the derived type's base-type reference.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BaseBench
                {
                    public required string {|#0:Text|} { get; set; }

                    [Benchmark]
                    public void Run() { }
                }

                public class DerivedBench : BaseBench
                {
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Text");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_SetsRequiredMembers_ctor_does_not_suppress_the_member_diagnostic()
        {
            // BDN reports the constructor separately (BDN1110) rather than propagating the attribute, so the member
            // BDN cannot set is still flagged.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Diagnostics.CodeAnalysis;

                namespace System.Diagnostics.CodeAnalysis
                {
                    internal sealed class SetsRequiredMembersAttribute : System.Attribute { }
                }

                public class BenchmarkClass
                {
                    public required string {|#0:Text|} { get; set; }

                    [SetsRequiredMembers]
                    public BenchmarkClass() { Text = ""; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Text");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_type_whose_benchmarks_use_a_derived_benchmark_attribute_is_analyzed()
        {
            // The runtime resolves [Benchmark] with GetCustomAttributes, so a user's own attribute deriving from it
            // still makes the type a benchmark - and its required members still have to be settable.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class CustomBenchmarkAttribute : BenchmarkAttribute { }

                public class BenchmarkClass
                {
                    public required string {|#0:Text|} { get; set; }

                    [CustomBenchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "Text");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_member_with_a_derived_params_attribute_does_not_report_error()
        {
            // BenchmarkDotNet resolves its attributes with GetCustomAttributes, which matches derived types, so a
            // user's own attribute deriving from [Params] is still set at construction.
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class CustomParamsAttribute : ParamsAttribute
                {
                    public CustomParamsAttribute(params object[] values) : base(values) { }
                }

                public class BenchmarkClass
                {
                    [CustomParams(1)]
                    public required int Value { get; set; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_required_member_in_a_non_benchmark_type_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                public class NotABenchmark
                {
                    public required string Text { get; set; }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }

    public class ConstructorMustNotSetRequiredMembers : AnalyzerTestFixture<RequiredMemberAnalyzer>
    {
        public ConstructorMustNotSetRequiredMembers() : base(RequiredMemberAnalyzer.ConstructorMustNotSetRequiredMembersRule) { }

        [Fact]
        public async Task A_SetsRequiredMembers_ctor_reports_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;
                using System.Diagnostics.CodeAnalysis;

                namespace System.Diagnostics.CodeAnalysis
                {
                    internal sealed class SetsRequiredMembersAttribute : System.Attribute { }
                }

                public class BenchmarkClass
                {
                    [Params(1)]
                    public required int Value { get; set; }

                    [SetsRequiredMembers]
                    public {|#0:BenchmarkClass|}() { Value = 1; }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            AddExpectedDiagnostic(0, DiagnosticSeverity.Error, "BenchmarkClass");
            DisableCompilerDiagnostics();
            await RunAsync();
        }

        [Fact]
        public async Task A_plain_ctor_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Attributes;

                public class BenchmarkClass
                {
                    [Params(1)]
                    public int Value { get; set; }

                    public BenchmarkClass() { }

                    [Benchmark]
                    public void Run() { }
                }
                """;
            TestCode = testCode;
            await RunAsync();
        }

        [Fact]
        public async Task A_SetsRequiredMembers_ctor_in_a_non_benchmark_type_does_not_report_error()
        {
            var testCode = /* lang=c#-test */ """
                using System.Diagnostics.CodeAnalysis;

                namespace System.Diagnostics.CodeAnalysis
                {
                    internal sealed class SetsRequiredMembersAttribute : System.Attribute { }
                }

                public class NotABenchmark
                {
                    public required string Text { get; set; }

                    [SetsRequiredMembers]
                    public NotABenchmark() { Text = ""; }
                }
                """;
            TestCode = testCode;
            DisableCompilerDiagnostics();
            await RunAsync();
        }
    }
}
