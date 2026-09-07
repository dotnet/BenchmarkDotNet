using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BenchmarkDotNet.IntegrationTests;

public class ConflictingNamesTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static IEnumerable<object[]> GetToolchains()
    {
        yield return [InProcessEmitToolchain.Default];

        if (ContinuousIntegration.IsGitHubDraftPR())
            yield break;

        yield return [Job.Default.GetToolchain()];
    }

    [Theory, MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void GeneratedMemberNamesCauseNoConflicts(IToolchain toolchain)
        => CanExecute<WithNamesUsedByGeneratedMembers>(ManualConfig.CreateEmpty()
            .AddJob(Job.Dry.WithToolchain(toolchain)));

    public class WithNamesUsedByGeneratedMembers
    {
        [Params(1)] public int Run { get; set; }
        [Params(2)] public int GetFieldsContainer { get; set; }
        [Params(3)] public int fieldsContainer { get; set; }
        [Params(4)] public int TrickTheJIT { get; set; }
        [Params(5)] public int Overhead { get; set; }
        [Params(6)] public int OverheadActionUnroll { get; set; }
        [Params(7)] public int OverheadActionNoUnroll { get; set; }
        [Params(8)] public int WorkloadActionUnroll { get; set; }
        [Params(9)] public int WorkloadActionNoUnroll { get; set; }
        [Params(10)] public int ForDisassemblyDiagnoser { get; set; }
        [Params(12)] public int GlobalCleanup { get; set; }
        [Params(14)] public int IterationCleanup { get; set; }
        [Params(15)] public int StartWorkload { get; set; }
        [Params(16)] public int WorkloadCore { get; set; }

        [GlobalSetup]
        public void GlobalSetup() { }

        public void GlobalSetup(int overload) { }

        [IterationSetup]
        public void IterationSetup() { }

        public void IterationSetup(int overload) { }

        // An argument, so that the benchmark actually has a fields container.
        [Benchmark]
        [Arguments(17)]
        public int System(int argument) => Run + GetFieldsContainer + fieldsContainer + TrickTheJIT + Overhead
            + OverheadActionUnroll + OverheadActionNoUnroll + WorkloadActionUnroll + WorkloadActionNoUnroll
            + ForDisassemblyDiagnoser + GlobalCleanup + IterationCleanup
            + StartWorkload + WorkloadCore + argument;

        // Not a compile-time constant, so the child process re-obtains it through an expression the renderer emits
        // - the only place the generated code names the BenchmarkDotNet namespace from inside this type.
        public static IEnumerable<object> NonConstantValues()
        {
            yield return new object();
        }

        [Benchmark]
        [ArgumentsSource(nameof(NonConstantValues))]
        public void BenchmarkDotNet(object argument) { }
    }
}
