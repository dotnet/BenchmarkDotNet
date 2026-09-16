using BenchmarkDotNet.Analyzers.General;
using BenchmarkDotNet.Analyzers.Tests.Fixtures;

namespace BenchmarkDotNet.Analyzers.Tests.AnalyzerTests.General;

public class RuntimeAndToolchainAnalyzerTests
{
    public class RuntimeAndToolchainBothSet : AnalyzerTestFixture<RuntimeAndToolchainAnalyzer>
    {
        public RuntimeAndToolchainBothSet() : base(RuntimeAndToolchainAnalyzer.RuntimeAndToolchainBothSetRule) { }

        [Fact]
        public async Task Chain_runtime_then_toolchain_triggers_diagnostic()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Environments;
                using BenchmarkDotNet.Jobs;
                using BenchmarkDotNet.Toolchains.InProcess.Emit;

                public class Config
                {
                    public Job Setup() => Job.Dry.{|#0:WithRuntime|}(CoreRuntime.Core80).WithToolchain(InProcessEmitToolchain.Default);
                }
                """;

            AddExpectedDiagnostic(0);
            await RunAsync();
        }

        [Fact]
        public async Task Chain_toolchain_then_runtime_triggers_diagnostic()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Environments;
                using BenchmarkDotNet.Jobs;
                using BenchmarkDotNet.Toolchains.InProcess.Emit;

                public class Config
                {
                    public Job Setup() => Job.Dry.WithToolchain(InProcessEmitToolchain.Default).{|#0:WithRuntime|}(CoreRuntime.Core80);
                }
                """;

            AddExpectedDiagnostic(0);
            await RunAsync();
        }

        [Fact]
        public async Task Chain_runtime_only_does_not_trigger_diagnostic()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Environments;
                using BenchmarkDotNet.Jobs;

                public class Config
                {
                    public Job Setup() => Job.Dry.WithRuntime(CoreRuntime.Core80);
                }
                """;

            await RunAsync();
        }

        [Fact]
        public async Task Chain_toolchain_only_does_not_trigger_diagnostic()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Jobs;
                using BenchmarkDotNet.Toolchains.InProcess.Emit;

                public class Config
                {
                    public Job Setup() => Job.Dry.WithToolchain(InProcessEmitToolchain.Default);
                }
                """;

            await RunAsync();
        }

        [Fact]
        public async Task Property_assignments_trigger_diagnostic_on_runtime()
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
                """;

            AddExpectedDiagnostic(0);
            await RunAsync();
        }

        [Fact]
        public async Task Object_initializer_triggers_diagnostic_on_runtime()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Environments;
                using BenchmarkDotNet.Jobs;
                using BenchmarkDotNet.Toolchains.InProcess.Emit;

                public class Config
                {
                    public Job Setup() => new Job { Infrastructure = { {|#0:Runtime = CoreRuntime.Core80|}, Toolchain = InProcessEmitToolchain.Default } };
                }
                """;

            AddExpectedDiagnostic(0);
            await RunAsync();
        }

        [Fact]
        public async Task Property_runtime_only_does_not_trigger_diagnostic()
        {
            TestCode = /* lang=c#-test */ """
                using BenchmarkDotNet.Environments;
                using BenchmarkDotNet.Jobs;

                public class Config
                {
                    public Job Setup()
                    {
                        var job = new Job();
                        job.Infrastructure.Runtime = CoreRuntime.Core80;
                        return job;
                    }
                }
                """;

            await RunAsync();
        }
    }
}
