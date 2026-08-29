using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using BenchmarkDotNet.CodeFixers;
using Microsoft.CodeAnalysis;

namespace BenchmarkDotNet.Analyzers.Tests.CodeFixTests;

public class RuntimeAndToolchainCodeFixProviderTests : CodeFixTestFixture<RuntimeAndToolchainAnalyzer, RuntimeAndToolchainCodeFixProvider>
{
    public RuntimeAndToolchainCodeFixProviderTests() : base(RuntimeAndToolchainAnalyzer.RuntimeAndToolchainBothSetRule) { }

    [Fact]
    public async Task CodeFix_removes_runtime_from_chain_runtime_then_toolchain()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => Job.Dry.{|#0:WithRuntime|}(CoreRuntime.Core80).WithToolchain(InProcessEmitToolchain.Default);
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => Job.Dry.WithToolchain(InProcessEmitToolchain.Default);
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_removes_runtime_from_chain_toolchain_then_runtime()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => Job.Dry.WithToolchain(InProcessEmitToolchain.Default).{|#0:WithRuntime|}(CoreRuntime.Core80);
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => Job.Dry.WithToolchain(InProcessEmitToolchain.Default);
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_removes_runtime_property_assignment_statement()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    {|#0:job.Infrastructure.Runtime = CoreRuntime.Core80|};
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_removes_runtime_from_object_initializer()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job { Infrastructure = { {|#0:Runtime = CoreRuntime.Core80|}, Toolchain = InProcessEmitToolchain.Default } };
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job { Infrastructure = { Toolchain = InProcessEmitToolchain.Default } };
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_keeps_the_comment_above_the_removed_statement_without_reindenting()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    // pin the runtime
                    {|#0:job.Infrastructure.Runtime = CoreRuntime.Core80|};
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    // pin the runtime
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_removes_a_top_level_statement()
    {
        // Under top-level statements the assignment is wrapped in a GlobalStatement; removing the inner statement
        // instead of the wrapper leaves it empty, which the syntax remover throws on.
        OutputKind = OutputKind.ConsoleApplication;

        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            var job = new Job();
            // pin the runtime
            {|#0:job.Infrastructure.Runtime = CoreRuntime.Core80|};
            job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            var job = new Job();
            // pin the runtime
            job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_is_not_offered_for_the_static_call_form()
    {
        // Dropping the link means replacing the invocation with its receiver, but here the receiver is the type, so
        // doing that would discard the job argument and leave JobExtensions.WithToolchain(t) - which does not compile.
        var code = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => JobExtensions.{|#0:WithRuntime|}(Job.Dry, CoreRuntime.Core80).WithToolchain(InProcessEmitToolchain.Default);
            }
            """.ReplaceLineEndings();

        TestCode = code;
        FixedCode = code;

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_is_not_offered_for_a_conditional_access_chain()
    {
        // The receiver of `job?.WithRuntime(r)` is not part of the invocation, so the link cannot be dropped by
        // replacing the call with it. The diagnostic covers the whole invocation here rather than just the method
        // name, because the analyzer's name lookup does not handle a member binding.
        var code = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup(Job job) => job?{|#0:.WithRuntime(CoreRuntime.Core80)|}.WithToolchain(InProcessEmitToolchain.Default);
            }
            """.ReplaceLineEndings();

        TestCode = code;
        FixedCode = code;

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_is_not_offered_for_an_embedded_statement()
    {
        // Removing the body of a brace-less `if` would leave it without one, and the syntax remover throws rather than
        // produce that, so no action is offered. The diagnostic still stands - FixedCode equals TestCode.
        var code = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup(bool condition)
                {
                    var job = new Job();
                    if (condition)
                        {|#0:job.Infrastructure.Runtime = CoreRuntime.Core80|};
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        TestCode = code;
        FixedCode = code;

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_keeps_the_comment_above_an_initializer_element()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job
                {
                    Infrastructure =
                    {
                        // pin the runtime
                        {|#0:Runtime = CoreRuntime.Core80|},
                        Toolchain = InProcessEmitToolchain.Default,
                    }
                };
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job
                {
                    Infrastructure =
                    {
                        // pin the runtime
                        Toolchain = InProcessEmitToolchain.Default,
                    }
                };
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_does_not_orphan_a_region_directive()
    {
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    #region runtime
                    {|#0:job.Infrastructure.Runtime = CoreRuntime.Core80|};
                    #endregion
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    #region runtime
                    #endregion
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_does_not_orphan_a_region_opened_inside_the_removed_statement()
    {
        // The #region sits in the leading trivia of an INTERIOR token of the statement, so it is inside the removed
        // span - KeepLeadingTrivia covers only the first token's trivia. Without KeepUnbalancedDirectives the
        // #endregion below is left orphaned and the fixed code does not compile (CS1028).
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    {|#0:job.Infrastructure.Runtime =
                    #region runtime
                        CoreRuntime.Core80|};
                    #endregion
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();

                    #region runtime
                    #endregion
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_does_not_orphan_a_region_whose_endregion_is_inside_the_removed_statement()
    {
        // The mirror image: the #endregion is the one inside the removed span, leaving the #region above it
        // unterminated (CS1038).
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    #region runtime
                    var job = new Job();
                    {|#0:job.Infrastructure.Runtime =
                    #endregion
                        CoreRuntime.Core80|};
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    #region runtime
                    var job = new Job();

                    #endregion
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_does_not_orphan_a_conditional_opened_inside_the_removed_statement()
    {
        // Same shape with #if/#endif. The condition has to be one that HOLDS for the analyzed compilation: DEBUG is
        // not defined there, so an "#if DEBUG" region would be disabled text and the test input itself would not
        // parse - the assignment would lose its right-hand side.
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();
                    {|#0:job.Infrastructure.Runtime =
                    #if !DEBUG
                        CoreRuntime.Core80|};
                    #endif
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup()
                {
                    var job = new Job();

                    #if !DEBUG
                    #endif
                    job.Infrastructure.Toolchain = InProcessEmitToolchain.Default;
                    return job;
                }
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }

    [Fact]
    public async Task CodeFix_does_not_orphan_a_conditional_opened_inside_a_removed_initializer_element()
    {
        // As above, the condition has to hold for the analyzed compilation: DEBUG is not defined there, so "#if
        // DEBUG" would make the initializer disabled text and the test input itself would not parse.
        TestCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job
                {
                    Infrastructure =
                    {
                        {|#0:Runtime =
                        #if !DEBUG
                            CoreRuntime.Core80|},
                        #endif
                        Toolchain = InProcessEmitToolchain.Default,
                    }
                };
            }
            """.ReplaceLineEndings();

        FixedCode = /* lang=c#-test */ """
            using BenchmarkDotNet.Environments;
            using BenchmarkDotNet.Jobs;
            using BenchmarkDotNet.Toolchains.InProcess.Emit;

            public class Config
            {
                public Job Setup() => new Job
                {
                    Infrastructure =
                    {

                        #if !DEBUG
                        #endif
                        Toolchain = InProcessEmitToolchain.Default,
                    }
                };
            }
            """.ReplaceLineEndings();

        AddExpectedDiagnostic(0);
        await RunAsync();
    }
}
