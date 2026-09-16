using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Mocks;

namespace BenchmarkDotNet.Toolchains;

/// <summary>
/// Test-only helper that resolves the toolchain for a <see cref="Job"/> without a real benchmark case. The core
/// only resolves toolchains per <c>BenchmarkCase</c> (which carries the full context); tests use this to inspect
/// the toolchain a parsed config job resolves to, or to get the default job's toolchain as a test parameter.
/// </summary>
internal static class JobToolchainExtensions
{
    // GetDefaultToolchain needs a BenchmarkCase (some runtimes inspect the descriptor's target assembly); tests have
    // no real benchmark here, so we wrap the job in one built from a mock descriptor. It's not a LinqPad assembly and
    // targets the same TFM as the test host, so it resolves to the same toolchain a bare job used to.
    private static readonly Descriptor MockDescriptor = new(MockFactory.MockType, MockFactory.MockMethodInfo);

    internal static IToolchain GetToolchain(this Job job)
    {
        if (job.Infrastructure.TryGetToolchain(out var toolchain))
            return toolchain;

        var benchmarkCase = BenchmarkCase.Create(MockDescriptor, job, ParameterInstances.Empty, ManualConfig.CreateEmpty().CreateImmutableConfig());
        return benchmarkCase.GetRuntime().GetDefaultToolchain(benchmarkCase);
    }
}
