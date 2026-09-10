using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.Environments
{
    /// <summary>
    /// The legacy (based on .Net Framework) Mono runtime.
    /// </summary>
    public sealed class MonoRuntime : LegacyMonoRuntime
    {
        public static readonly MonoRuntime Default = new();

        public override string Name => "Mono";

        private MonoRuntime() { }

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
            => RoslynMonoToolchain.Default;
    }
}
