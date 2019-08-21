using System;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class CoreRuntime : Runtime
    {
        public static readonly CoreRuntime Core20 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp20, "netcoreapp2.0", ".NET Core 2.0");
        public static readonly CoreRuntime Core21 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp21, "netcoreapp2.1", ".NET Core 2.1");
        public static readonly CoreRuntime Core22 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp22, "netcoreapp2.2", ".NET Core 2.2");
        public static readonly CoreRuntime Core30 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp30, "netcoreapp3.0", ".NET Core 3.0");
        public static readonly CoreRuntime Core31 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp31, "netcoreapp3.1", ".NET Core 3.1");
        public static readonly CoreRuntime Core50 = new CoreRuntime(TargetFrameworkMoniker.NetCoreApp50, "netcoreapp5.0", ".NET Core 5.0");

        private CoreRuntime(TargetFrameworkMoniker targetFrameworkMoniker, string msBuildMoniker, string displayName)
            : base(targetFrameworkMoniker, msBuildMoniker, displayName)
        {
        }

        /// <summary>
        /// use this method if you want to target .NET Core version not supported by current version of BenchmarkDotNet. Example: .NET Core 10
        /// </summary>
        /// <param name="msBuildMoniker">msbuild moniker, example: netcoreapp10.0</param>
        /// <param name="displayName">display name used by BDN to print the results</param>
        /// <returns>new runtime information</returns>
        public static CoreRuntime CreateForNewVersion(string msBuildMoniker, string displayName)
        {
            if (string.IsNullOrEmpty(msBuildMoniker)) throw new ArgumentNullException(nameof(msBuildMoniker));
            if (string.IsNullOrEmpty(displayName)) throw new ArgumentNullException(nameof(displayName));

            return new CoreRuntime(TargetFrameworkMoniker.NotRecognized, msBuildMoniker, displayName);
        }

        internal static CoreRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of .NET Core if the process is not a .NET Core process!");
            }

            string netCoreAppVersion = RuntimeInformation.GetNetCoreVersion();

            string[] versionNumbers = netCoreAppVersion.Split('.');
            string msBuildMoniker = $"netcoreapp{versionNumbers[0]}.{versionNumbers[1]}";
            string displayName = $".NET Core {versionNumbers[0]}.{versionNumbers[1]}";

            switch (msBuildMoniker)
            {
                case "netcoreapp2.0": return Core20;
                case "netcoreapp2.1": return Core21;
                case "netcoreapp2.2": return Core22;
                case "netcoreapp3.0": return Core30;
                case "netcoreapp3.1": return Core31;
                case "netcoreapp5.0": return Core50;
                default: // support future version of .NET Core
                    return CreateForNewVersion(msBuildMoniker, displayName); 
            }
        }
    }
}
