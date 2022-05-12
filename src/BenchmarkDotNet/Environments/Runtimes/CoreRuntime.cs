using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class CoreRuntime : Runtime
    {
        public static readonly CoreRuntime Core20 = new CoreRuntime(RuntimeMoniker.NetCoreApp20, "netcoreapp2.0", ".NET Core 2.0");
        public static readonly CoreRuntime Core21 = new CoreRuntime(RuntimeMoniker.NetCoreApp21, "netcoreapp2.1", ".NET Core 2.1");
        public static readonly CoreRuntime Core22 = new CoreRuntime(RuntimeMoniker.NetCoreApp22, "netcoreapp2.2", ".NET Core 2.2");
        public static readonly CoreRuntime Core30 = new CoreRuntime(RuntimeMoniker.NetCoreApp30, "netcoreapp3.0", ".NET Core 3.0");
        public static readonly CoreRuntime Core31 = new CoreRuntime(RuntimeMoniker.NetCoreApp31, "netcoreapp3.1", ".NET Core 3.1");
        public static readonly CoreRuntime Core50 = new CoreRuntime(RuntimeMoniker.Net50, "net5.0", ".NET 5.0");
        public static readonly CoreRuntime Core60 = new CoreRuntime(RuntimeMoniker.Net60, "net6.0", ".NET 6.0");
        public static readonly CoreRuntime Core70 = new CoreRuntime(RuntimeMoniker.Net70, "net7.0", ".NET 7.0");

        public static CoreRuntime Latest => Core70; // when dotnet/runtime branches for 8.0, this will need to get updated

        private CoreRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }

        public bool IsPlatformSpecific => MsBuildMoniker.IndexOf('-') > 0;

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

            return new CoreRuntime(RuntimeMoniker.NotRecognized, msBuildMoniker, displayName);
        }

        internal static CoreRuntime GetCurrentVersion()
        {
            if (!RuntimeInformation.IsNetCore)
            {
                throw new NotSupportedException("It's impossible to reliably detect the version of .NET Core if the process is not a .NET Core process!");
            }

            if (!TryGetVersion(out Version version))
            {
                throw new NotSupportedException("Unable to recognize .NET Core version, please report a bug at https://github.com/dotnet/BenchmarkDotNet");
            }

            return FromVersion(version);
        }

        internal static CoreRuntime FromVersion(Version version)
        {
            switch (version)
            {
                case Version v when v.Major == 2 && v.Minor == 0: return Core20;
                case Version v when v.Major == 2 && v.Minor == 1: return Core21;
                case Version v when v.Major == 2 && v.Minor == 2: return Core22;
                case Version v when v.Major == 3 && v.Minor == 0: return Core30;
                case Version v when v.Major == 3 && v.Minor == 1: return Core31;
                case Version v when v.Major == 5 && v.Minor == 0: return GetPlatformSpecific(Core50);
                case Version v when v.Major == 6 && v.Minor == 0: return GetPlatformSpecific(Core60);
                case Version v when v.Major == 7 && v.Minor == 0: return GetPlatformSpecific(Core70);
                default:
                    return CreateForNewVersion($"net{version.Major}.{version.Minor}", $".NET {version.Major}.{version.Minor}");
            }
        }

        internal static bool TryGetVersion(out Version version)
        {
            // we can't just use System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
            // because it can be null and it reports versions like 4.6.* for .NET Core 2.*

            // for .NET 5+ we can use Environment.Version
            if (Environment.Version.Major >= 5)
            {
                version = Environment.Version;
                return true;
            }

            string runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            if (TryGetVersionFromRuntimeDirectory(runtimeDirectory, out version))
            {
                return true;
            }

            var systemPrivateCoreLib = FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location);
            // systemPrivateCoreLib.Product*Part properties return 0 so we have to implement some ugly parsing...
            if (TryGetVersionFromProductInfo(systemPrivateCoreLib.ProductVersion, systemPrivateCoreLib.ProductName, out version))
            {
                return true;
            }

            // it's OK to use this method only after checking the previous ones
            // because we might have a benchmark app build for .NET Core X but executed using CoreRun Y
            // example: -f netcoreapp3.1 --corerun $omittedForBrevity\Microsoft.NETCore.App\6.0.0\CoreRun.exe - built as 3.1, run as 6.0 (#1576)
            string frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            if (TryGetVersionFromFrameworkName(frameworkName, out version))
            {
                return true;
            }

            if (RuntimeInformation.IsRunningInContainer)
            {
                return Version.TryParse(Environment.GetEnvironmentVariable("DOTNET_VERSION"), out version)
                    || Version.TryParse(Environment.GetEnvironmentVariable("ASPNETCORE_VERSION"), out version);
            }

            version = null;
            return false;
        }

        // sample input:
        // for dotnet run: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.12\
        // for dotnet publish: C:\Users\adsitnik\source\repos\ConsoleApp25\ConsoleApp25\bin\Release\netcoreapp2.0\win-x64\publish\
        internal static bool TryGetVersionFromRuntimeDirectory(string runtimeDirectory, out Version version)
        {
            if (!string.IsNullOrEmpty(runtimeDirectory) && Version.TryParse(GetParsableVersionPart(new DirectoryInfo(runtimeDirectory).Name), out version))
            {
                return true;
            }

            version = null;
            return false;
        }

        // sample input:
        // 2.0: 4.6.26614.01 @BuiltBy: dlab14-DDVSOWINAGE018 @Commit: a536e7eec55c538c94639cefe295aa672996bf9b, Microsoft .NET Framework
        // 2.1: 4.6.27817.01 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.1 @SrcCode: https://github.com/dotnet/coreclr/tree/6f78fbb3f964b4f407a2efb713a186384a167e5c, Microsoft .NET Framework
        // 2.2: 4.6.27817.03 @BuiltBy: dlab14-DDVSOWINAGE101 @Branch: release/2.2 @SrcCode: https://github.com/dotnet/coreclr/tree/ce1d090d33b400a25620c0145046471495067cc7, Microsoft .NET Framework
        // 3.0: 3.0.0-preview8.19379.2+ac25be694a5385a6a1496db40de932df0689b742, Microsoft .NET Core
        // 5.0: 5.0.0-alpha1.19413.7+0ecefa44c9d66adb8a997d5778dc6c246ad393a7, Microsoft .NET Core
        internal static bool TryGetVersionFromProductInfo(string productVersion, string productName, out Version version)
        {
            if (!string.IsNullOrEmpty(productVersion) && !string.IsNullOrEmpty(productName))
            {
                if (productName.IndexOf(".NET Core", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string parsableVersion = GetParsableVersionPart(productVersion);
                    if (Version.TryParse(productVersion, out version) || Version.TryParse(parsableVersion, out version))
                    {
                        return true;
                    }
                }

                // yes, .NET Core 2.X has a product name == .NET Framework...
                if (productName.IndexOf(".NET Framework", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    const string releaseVersionPrefix = "release/";
                    int releaseVersionIndex = productVersion.IndexOf(releaseVersionPrefix);
                    if (releaseVersionIndex > 0)
                    {
                        string releaseVersion = GetParsableVersionPart(productVersion.Substring(releaseVersionIndex + releaseVersionPrefix.Length));

                        return Version.TryParse(releaseVersion, out version);
                    }
                }
            }

            version = null;
            return false;
        }

        // sample input:
        // .NETCoreApp,Version=v2.0
        // .NETCoreApp,Version=v2.1
        internal static bool TryGetVersionFromFrameworkName(string frameworkName, out Version version)
        {
            const string versionPrefix = ".NETCoreApp,Version=v";
            if (!string.IsNullOrEmpty(frameworkName) && frameworkName.StartsWith(versionPrefix))
            {
                string frameworkVersion = GetParsableVersionPart(frameworkName.Substring(versionPrefix.Length));

                return Version.TryParse(frameworkVersion, out version);
            }

            version = null;
            return false;
        }

        // Version.TryParse does not handle thing like 3.0.0-WORD
        private static string GetParsableVersionPart(string fullVersionName) => new string(fullVersionName.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

        private static CoreRuntime GetPlatformSpecific(CoreRuntime fallback)
        {
            // TargetPlatformAttribute is not part of .NET Standard 2.0 so as usuall we have to use some reflection hacks...
            var targetPlatformAttributeType = typeof(object).Assembly.GetType("System.Runtime.Versioning.TargetPlatformAttribute", throwOnError: false);
            if (targetPlatformAttributeType is null) // an old preview version of .NET 5
                return fallback;

            var exe = Assembly.GetEntryAssembly();
            if (exe is null)
                return fallback;

            var attributeInstance = exe.GetCustomAttribute(targetPlatformAttributeType);
            if (attributeInstance is null)
                return fallback;

            var platformNameProperty = targetPlatformAttributeType.GetProperty("PlatformName");
            if (platformNameProperty is null)
                return fallback;

            if (!(platformNameProperty.GetValue(attributeInstance) is string platformName))
                return fallback;

            return new CoreRuntime(fallback.RuntimeMoniker, $"{fallback.MsBuildMoniker}-{platformName}", fallback.Name);
        }
    }
}
