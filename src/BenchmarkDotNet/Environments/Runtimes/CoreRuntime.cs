using System;
using System.Diagnostics;
using System.Linq;
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
            if (!string.IsNullOrEmpty(netCoreAppVersion) && Version.TryParse(netCoreAppVersion, out _))
            {
                return FromNetCoreAppVersion(netCoreAppVersion);
            }

            var coreclrAssemblyInfo = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
            // coreclrAssemblyInfo.Product*Part properties return 0 so we have to implement some ugly parsing...
            return FromProductVersion(coreclrAssemblyInfo.ProductVersion, coreclrAssemblyInfo.ProductName); 
        }

        internal static CoreRuntime FromNetCoreAppVersion(string netCoreAppVersion)
        {
            var version = Version.Parse(netCoreAppVersion);

            string msBuildMoniker = $"netcoreapp{version.Major}.{version.Minor}";
            string displayName = $".NET Core {version.Major}.{version.Minor}";

            return FromMoniker(msBuildMoniker, displayName);
        }

        // sample input: 
        // 2.0: 4.6.26614.01 @BuiltBy: dlab14-DDVSOWINAGE018 @Commit: a536e7eec55c538c94639cefe295aa672996bf9b, Microsoft .NET Framework
        // 2.1: 4.6.27817.01 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.1 @SrcCode: https://github.com/dotnet/coreclr/tree/6f78fbb3f964b4f407a2efb713a186384a167e5c, Microsoft .NET Framework
        // 2.2: 4.6.27817.03 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.2 @SrcCode: https://github.com/dotnet/coreclr/tree/ce1d090d33b400a25620c0145046471495067cc7, Microsoft .NET Framework
        // 3.0: 3.0.0-preview8.19379.2+ac25be694a5385a6a1496db40de932df0689b742, Microsoft .NET Core
        // 5.0: 5.0.0-alpha1.19413.7+0ecefa44c9d66adb8a997d5778dc6c246ad393a7, Microsoft .NET Core
        internal static CoreRuntime FromProductVersion(string productVersion, string productName)
        {
            if (productName.IndexOf(".NET Core", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Version.TryParse does not handle thing like 3.0.0-WORD
                string parsableVersion = new string(productVersion.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
                if (Version.TryParse(productVersion, out var version) || Version.TryParse(parsableVersion, out version))
                {
                    return FromMoniker($"netcoreapp{version.Major}.{version.Minor}", $".NET Core {version.Major}.{version.Minor}");
                }
            }

            // yes, .NET Core 2.X has a product name == .NET Framework...
            if (productName.IndexOf(".NET Framework", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                const string releaseVersionPrefix = "release/";
                int releaseVersionIndex = productVersion.IndexOf(releaseVersionPrefix);
                if (releaseVersionIndex > 0)
                {
                    string releaseVersion = new string(productVersion.Skip(releaseVersionIndex + releaseVersionPrefix.Length).TakeWhile(c => !char.IsWhiteSpace(c)).ToArray());

                    return FromMoniker($"netcoreapp{releaseVersion}", $".NET Core {releaseVersion}");
                }

                // BenchmarkDotNet targets .NET Standard 2.0, so it's safe to asume that it can be only .NET Core 2.0 now..
                return Core20;
            }

            throw new NotSupportedException($"Unable to recognize .NET Core version, the productVersion was {productVersion}, productName was {productName}");
        }

        private static CoreRuntime FromMoniker(string msBuildMoniker, string displayName)
        {
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
