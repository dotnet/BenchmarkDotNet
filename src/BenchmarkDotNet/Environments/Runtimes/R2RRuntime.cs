using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.R2R;

namespace BenchmarkDotNet.Environments
{
    public sealed class R2RRuntime : Runtime
    {
        public static readonly R2RRuntime Net80 = new(new(8, 0));
        public static readonly R2RRuntime Net90 = new(new(9, 0));
        public static readonly R2RRuntime Net10_0 = new(new(10, 0));
        public static readonly R2RRuntime Net11_0 = new(new(11, 0));

        private R2RRuntime(Version version) => Version = ToRuntimeVersion(version);

        public override string Name => "R2R";

        public override Version Version { get; }

        /// <summary>Returns a runtime for the given version.</summary>
        public static R2RRuntime From(Version version)
            => version.Major switch
            {
                8 => Net80,
                9 => Net90,
                10 => Net10_0,
                11 => Net11_0,
                _ => new(version),
            };

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
            => CsProjR2RToolchain.From(this, R2RSettings.Default);
    }
}
