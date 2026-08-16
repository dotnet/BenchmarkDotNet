using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;
using BenchmarkDotNet.CodeFixers;

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
}
