using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Environments
{
    public class CoreRuntime : Runtime
    {
        public static readonly CoreRuntime Core20 = new(RuntimeMoniker.NetCoreApp20, "netcoreapp2.0", ".NET Core 2.0");
        public static readonly CoreRuntime Core21 = new(RuntimeMoniker.NetCoreApp21, "netcoreapp2.1", ".NET Core 2.1");
        public static readonly CoreRuntime Core22 = new(RuntimeMoniker.NetCoreApp22, "netcoreapp2.2", ".NET Core 2.2");
        public static readonly CoreRuntime Core30 = new(RuntimeMoniker.NetCoreApp30, "netcoreapp3.0", ".NET Core 3.0");
        public static readonly CoreRuntime Core31 = new(RuntimeMoniker.NetCoreApp31, "netcoreapp3.1", ".NET Core 3.1");
        public static readonly CoreRuntime Core50 = new(RuntimeMoniker.Net50, "net5.0", ".NET 5.0");
        public static readonly CoreRuntime Core60 = new(RuntimeMoniker.Net60, "net6.0", ".NET 6.0");
        public static readonly CoreRuntime Core70 = new(RuntimeMoniker.Net70, "net7.0", ".NET 7.0");
        public static readonly CoreRuntime Core80 = new(RuntimeMoniker.Net80, "net8.0", ".NET 8.0");
        public static readonly CoreRuntime Core90 = new(RuntimeMoniker.Net90, "net9.0", ".NET 9.0");
        public static readonly CoreRuntime Core10_0 = new(RuntimeMoniker.Net10_0, "net10.0", ".NET 10.0");

        public static CoreRuntime Latest => Core10_0; // when dotnet/runtime branches for 11.0, this will need to get updated

        private CoreRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }

        public bool IsPlatformSpecific => MsBuildMoniker.IndexOf('-') > 0;

        /// <summary>
        /// use this method if you want to target .NET version not supported by current version of BenchmarkDotNet. Example: .NET 10
        /// </summary>
        /// <param name="msBuildMoniker">msbuild moniker, example: net10.0</param>
        /// <param name="displayName">display name used by BDN to print the results</param>
        /// <returns>new runtime information</returns>
        public static CoreRuntime CreateForNewVersion(string msBuildMoniker, string displayName)
        {
            if (string.IsNullOrEmpty(msBuildMoniker)) throw new ArgumentNullException(nameof(msBuildMoniker));
            if (string.IsNullOrEmpty(displayName)) throw new ArgumentNullException(nameof(displayName));

            return new CoreRuntime(RuntimeMoniker.NotRecognized, msBuildMoniker, displayName);
        }

        internal static CoreRuntime GetTargetOrCurrentVersion(Assembly? assembly)
            // Try to determine the version that the assembly was compiled for.
            => FrameworkVersionHelper.GetTargetCoreVersion(assembly) is { } version
                ? FromVersion(version, assembly)
                // Fallback to the current running version.
                : GetCurrentVersion();

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

            return FromVersion(version, null);
        }

        internal static CoreRuntime FromVersion(Version version, Assembly? assembly = null) => version switch
        {
            { Major: 2, Minor: 0 } => Core20,
            { Major: 2, Minor: 1 } => Core21,
            { Major: 2, Minor: 2 } => Core22,
            { Major: 3, Minor: 0 } => Core30,
            { Major: 3, Minor: 1 } => Core31,
            { Major: 5 } => GetPlatformSpecific(Core50, assembly),
            { Major: 6 } => GetPlatformSpecific(Core60, assembly),
            { Major: 7 } => GetPlatformSpecific(Core70, assembly),
            { Major: 8 } => GetPlatformSpecific(Core80, assembly),
            { Major: 9 } => GetPlatformSpecific(Core90, assembly),
            { Major: 10 } => GetPlatformSpecific(Core10_0, assembly),
            _ => CreateForNewVersion($"net{version.Major}.{version.Minor}", $".NET {version.Major}.{version.Minor}"),
        };

        internal static bool TryGetVersion(out Version? version)
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

            string coreclrLocation = typeof(object).Assembly.Location;
            // Single-file publish has empty assembly location.
            if (coreclrLocation.IsNotBlank())
            {
                var systemPrivateCoreLib = FileVersionInfo.GetVersionInfo(coreclrLocation);
                // systemPrivateCoreLib.Product*Part properties return 0 so we have to implement some ugly parsing...
                if (TryGetVersionFromProductInfo(systemPrivateCoreLib.ProductVersion, systemPrivateCoreLib.ProductName, out version))
                {
                    return true;
                }
            }
            else
            {
                // .Net Core 3.X supports single-file publish, .Net Core 2.X does not.
                // .Net Core 3.X fixed the version in FrameworkDescription, so we don't need to handle the case of 4.6.x in this branch.
                var frameworkDescriptionVersion = GetParsableVersionPart(GetVersionFromFrameworkDescription());
                if (Version.TryParse(frameworkDescriptionVersion, out version))
                {
                    return true;
                }
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

        internal static string GetVersionFromFrameworkDescription()
        {
            // .NET 10.0.0-preview.5.25277.114 -> 10.0.0-preview.5.25277.114
            // .NET Core 3.1.32 -> 3.1.32
            string frameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            return new string(frameworkDescription.SkipWhile(c => !char.IsDigit(c)).ToArray());
        }

        // sample input:
        // for dotnet run: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.12\
        // for dotnet publish: C:\Users\adsitnik\source\repos\ConsoleApp25\ConsoleApp25\bin\Release\netcoreapp2.0\win-x64\publish\
        internal static bool TryGetVersionFromRuntimeDirectory(string runtimeDirectory, out Version? version)
        {
            if (runtimeDirectory.IsNotBlank() && Version.TryParse(GetParsableVersionPart(new DirectoryInfo(runtimeDirectory).Name), out version))
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
        internal static bool TryGetVersionFromProductInfo(string productVersion, string productName, out Version? version)
        {
            if (productVersion.IsNotBlank() && productName.IsNotBlank())
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
                    int releaseVersionIndex = productVersion.IndexOf(releaseVersionPrefix, StringComparison.Ordinal);
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
        internal static bool TryGetVersionFromFrameworkName(string frameworkName, out Version? version)
        {
            const string versionPrefix = ".NETCoreApp,Version=v";
            if (frameworkName.IsNotBlank() && frameworkName.StartsWith(versionPrefix))
            {
                string frameworkVersion = GetParsableVersionPart(frameworkName.Substring(versionPrefix.Length));

                return Version.TryParse(frameworkVersion, out version);
            }

            version = null;
            return false;
        }

        // Version.TryParse does not handle thing like 3.0.0-WORD
        internal static string GetParsableVersionPart(string fullVersionName) => new string(fullVersionName.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());

        private static CoreRuntime GetPlatformSpecific(CoreRuntime fallback, Assembly? assembly)
            => TryGetTargetPlatform(assembly ?? Assembly.GetEntryAssembly(), out var platform)
                ? new CoreRuntime(fallback.RuntimeMoniker, $"{fallback.MsBuildMoniker}-{platform}", fallback.Name)
                : fallback;

        internal static bool TryGetTargetPlatform(Assembly? assembly, [NotNullWhen(true)] out string? platform)
        {
            platform = null;

            if (assembly is null)
                return false;

            // TargetPlatformAttribute is not part of .NET Standard 2.0 so as usual we have to use some reflection hacks.
            var targetPlatformAttributeType = typeof(object).Assembly.GetType("System.Runtime.Versioning.TargetPlatformAttribute", throwOnError: false);
            if (targetPlatformAttributeType is null) // an old preview version of .NET 5
                return false;

            var attributeInstance = assembly.GetCustomAttribute(targetPlatformAttributeType);
            if (attributeInstance is null)
                return false;

            var platformNameProperty = targetPlatformAttributeType.GetProperty("PlatformName");
            if (platformNameProperty is null)
                return false;

            if (platformNameProperty.GetValue(attributeInstance) is not string platformName)
                return false;

            platform = platformName;
            return true;
        }
    }
}
