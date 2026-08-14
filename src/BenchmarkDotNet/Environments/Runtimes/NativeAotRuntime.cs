using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.NativeAot;

namespace BenchmarkDotNet.Environments
{
    public sealed class NativeAotRuntime : Runtime
    {
        /// <summary>
        /// NativeAOT compiled as net7.0
        /// </summary>
        public static readonly NativeAotRuntime Net70 = new(new(7, 0));
        /// <summary>
        /// NativeAOT compiled as net8.0
        /// </summary>
        public static readonly NativeAotRuntime Net80 = new(new(8, 0));
        /// <summary>
        /// NativeAOT compiled as net9.0
        /// </summary>
        public static readonly NativeAotRuntime Net90 = new(new(9, 0));
        /// <summary>
        /// NativeAOT compiled as net10.0
        /// </summary>
        public static readonly NativeAotRuntime Net10_0 = new(new(10, 0));
        /// <summary>
        /// NativeAOT compiled as net11.0
        /// </summary>
        public static readonly NativeAotRuntime Net11_0 = new(new(11, 0));

        private NativeAotRuntime(Version version) => Version = version;

        public override string Name => "NativeAOT";

        public override Version Version { get; }

        internal static NativeAotRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore && !RuntimeInformation.IsNativeAOT)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of NativeAOT if the process is not a .NET or NativeAOT process!");
            }

            return From(Environment.Version);
        }

        /// <summary>Returns a runtime for the given version.</summary>
        public static NativeAotRuntime From(Version version)
            => version.Major switch
            {
                7 => Net70,
                8 => Net80,
                9 => Net90,
                10 => Net10_0,
                11 => Net11_0,
                _ => new(version),
            };

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
            => CsProjNativeAotToolchain.From(this, NativeAotSettings.Default);
    }
}
