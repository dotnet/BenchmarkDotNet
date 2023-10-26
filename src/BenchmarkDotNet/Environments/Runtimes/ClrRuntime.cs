using System;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

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

        private ClrRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName, string? version = null)
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

        public override bool Equals(object obj) => obj is ClrRuntime other && Equals(other);

        public bool Equals(ClrRuntime other) => other != null && base.Equals(other) && Version == other.Version;

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Version);

        internal static ClrRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsWindows())
            {
                throw new NotSupportedException(".NET Framework supports Windows OS only.");
            }

            // this logic is put to a separate method to avoid any assembly loading issues on non Windows systems
            string sdkVersion = FrameworkVersionHelper.GetLatestNetDeveloperPackVersion();

            string version = sdkVersion
                ?? FrameworkVersionHelper.GetFrameworkReleaseVersion(); // .NET Developer Pack is not installed

            switch (version)
            {
                case "4.6.1": return Net461;
                case "4.6.2": return Net462;
                case "4.7":   return Net47;
                case "4.7.1": return Net471;
                case "4.7.2": return Net472;
                case "4.8":   return Net48;
                case "4.8.1": return Net481;
                default: // unlikely to happen but theoretically possible
                    return new ClrRuntime(RuntimeMoniker.NotRecognized, $"net{version.Replace(".", null)}", $".NET Framework {version}");
            }
        }
    }
}