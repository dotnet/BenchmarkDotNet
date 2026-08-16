using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Framework;
using System.Reflection;

namespace BenchmarkDotNet.Environments
{
    public sealed class ClrRuntime : Runtime
    {
        public static readonly ClrRuntime Net461 = new(new(4, 6, 1));
        public static readonly ClrRuntime Net462 = new(new(4, 6, 2));
        public static readonly ClrRuntime Net47 = new(new(4, 7));
        public static readonly ClrRuntime Net471 = new(new(4, 7, 1));
        public static readonly ClrRuntime Net472 = new(new(4, 7, 2));
        public static readonly ClrRuntime Net48 = new(new(4, 8));
        public static readonly ClrRuntime Net481 = new(new(4, 8, 1));

        public override string Name => ".NET Framework";
        public override Version Version { get; }

        private ClrRuntime(Version version) => Version = version;

        internal static ClrRuntime GetCurrentVersion()
        {
            if (!OsDetector.IsWindows())
            {
                throw new PlatformNotSupportedException(".NET Framework supports Windows OS only.");
            }

            var version = FrameworkVersionHelper.GetLatestNetDeveloperPackVersion()
                ?? FrameworkVersionHelper.GetFrameworkReleaseVersion(); // .NET Developer Pack is not installed
            return FromVersion(version);
        }

        internal static ClrRuntime GetTargetOrCurrentVersion(Assembly? assembly)
        {
            if (!OsDetector.IsWindows())
            {
                throw new PlatformNotSupportedException(".NET Framework supports Windows OS only.");
            }

            // Try to determine the Framework version that the assembly was compiled for.
            var version = FrameworkVersionHelper.GetTargetFrameworkVersion(assembly);
            return version != null
                ? FromVersion(version)
                // Fallback to the current running Framework version.
                : GetCurrentVersion();
        }

        internal static ClrRuntime FromVersion(Version version)
            => (version.Major, version.Minor, version.Build) switch
            {
                (4, 8, 1) => Net481,
                (4, 8, _) => Net48,
                (4, 7, 2) => Net472,
                (4, 7, 1) => Net471,
                (4, 7, _) => Net47,
                (4, 6, 2) => Net462,
                (4, 6, 1) => Net461,
                // unlikely to happen but theoretically possible
                _ => new ClrRuntime(version),
            };

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        {
            if (!benchmarkCase.Job.HasDynamicBuildCharacteristic() && RuntimeInformation.IsFullFramework && Equals(GetTargetOrCurrentVersion(benchmarkCase.Descriptor.Type.Assembly)))
                // The in-place Roslyn toolchain runs the already-loaded assembly. When the requested version differs
                // from the running framework, report the requested one so the job and summary stay consistent.
                return RoslynFrameworkToolchain.From(this);

            return CsProjFrameworkToolchain.From(this, FrameworkSettings.Default);
        }
    }
}
