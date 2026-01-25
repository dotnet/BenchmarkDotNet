using System;
using System.Reflection;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;

#nullable enable

namespace BenchmarkDotNet.Environments
{
    public class ClrRuntime : Runtime, IEquatable<ClrRuntime>
    {
        public static readonly ClrRuntime Net461 = new ClrRuntime(RuntimeMoniker.Net461, "net461", ".NET Framework 4.6.1");
        public static readonly ClrRuntime Net462 = new ClrRuntime(RuntimeMoniker.Net462, "net462", ".NET Framework 4.6.2");
        public static readonly ClrRuntime Net47 = new ClrRuntime(RuntimeMoniker.Net47, "net47", ".NET Framework 4.7");
        public static readonly ClrRuntime Net471 = new ClrRuntime(RuntimeMoniker.Net471, "net471", ".NET Framework 4.7.1");
        public static readonly ClrRuntime Net472 = new ClrRuntime(RuntimeMoniker.Net472, "net472", ".NET Framework 4.7.2");
        public static readonly ClrRuntime Net48 = new ClrRuntime(RuntimeMoniker.Net48, "net48", ".NET Framework 4.8");
        public static readonly ClrRuntime Net481 = new ClrRuntime(RuntimeMoniker.Net481, "net481", ".NET Framework 4.8.1");

        public string Version { get; }

        private ClrRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName, string version = "")
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
            Version = version;
        }

        /// <param name="version">YOU PROBABLY DON'T NEED IT, but if you are a .NET Runtime developer..
        /// please set it to particular .NET Runtime version if you want to benchmark it.
        /// BenchmarkDotNet in going to pass `COMPLUS_Version` env var to the process for you.
        /// </param>
        public static ClrRuntime CreateForLocalFullNetFrameworkBuild(string version)
        {
            if (string.IsNullOrEmpty(version)) throw new ArgumentNullException(nameof(version));

            var current = GetCurrentVersion();

            return new ClrRuntime(current.RuntimeMoniker, current.MsBuildMoniker, version, version);
        }

        public override bool Equals(object? obj) => obj is ClrRuntime other && Equals(other);

        public bool Equals(ClrRuntime? other) => other != null && base.Equals(other) && Version == other.Version;

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Version);

        internal static ClrRuntime GetCurrentVersion()
        {
            if (!OsDetector.IsWindows())
            {
                throw new PlatformNotSupportedException(".NET Framework supports Windows OS only.");
            }

            string version = FrameworkVersionHelper.GetLatestNetDeveloperPackVersion()
                ?? FrameworkVersionHelper.GetFrameworkReleaseVersion(); // .NET Developer Pack is not installed
            return GetRuntimeFromVersion(version);
        }

        internal static ClrRuntime GetTargetOrCurrentVersion(Assembly? assembly)
        {
            if (!OsDetector.IsWindows())
            {
                throw new PlatformNotSupportedException(".NET Framework supports Windows OS only.");
            }

            // Try to determine the Framework version that the assembly was compiled for.
            string? version = FrameworkVersionHelper.GetTargetFrameworkVersion(assembly);
            return version != null
                ? GetRuntimeFromVersion(version)
                // Fallback to the current running Framework version.
                : GetCurrentVersion();
        }

        private static ClrRuntime GetRuntimeFromVersion(string version)
            => version switch
            {
                "4.6.1" => Net461,
                "4.6.2" => Net462,
                "4.7" => Net47,
                "4.7.1" => Net471,
                "4.7.2" => Net472,
                "4.8" => Net48,
                "4.8.1" => Net481,
                // unlikely to happen but theoretically possible
                _ => new ClrRuntime(RuntimeMoniker.NotRecognized, $"net{version.Replace(".", null)}", $".NET Framework {version}"),
            };
    }
}