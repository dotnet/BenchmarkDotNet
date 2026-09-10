using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.NetCoreApp;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;

namespace BenchmarkDotNet.Environments
{
    /// <summary>
    /// Represents a specific version of the .NET (Core) runtime.
    /// </summary>
    public sealed class CoreRuntime : Runtime
    {
        public static readonly CoreRuntime Core20 = new(new(2, 0));
        public static readonly CoreRuntime Core21 = new(new(2, 1));
        public static readonly CoreRuntime Core22 = new(new(2, 2));
        public static readonly CoreRuntime Core30 = new(new(3, 0));
        public static readonly CoreRuntime Core31 = new(new(3, 1));
        public static readonly CoreRuntime Core50 = new(new(5, 0));
        public static readonly CoreRuntime Core60 = new(new(6, 0));
        public static readonly CoreRuntime Core70 = new(new(7, 0));
        public static readonly CoreRuntime Core80 = new(new(8, 0));
        public static readonly CoreRuntime Core90 = new(new(9, 0));
        public static readonly CoreRuntime Core10_0 = new(new(10, 0));
        public static readonly CoreRuntime Core11_0 = new(new(11, 0));

        public static CoreRuntime Latest => Core11_0; // when dotnet/runtime branches for 12.0, this will need to get updated

        private readonly string? platform;

        private CoreRuntime(Version version, string? platform = null)
        {
            Version = ToRuntimeVersion(version);
            this.platform = platform;
            Name = version.Major < 5 ? ".NET Core" : ".NET";
        }

        public override string Name { get; }

        public override Version Version { get; }

        public bool IsPlatformSpecific => platform.IsNotBlank();

        public string? Platform => platform;

        // The base compares Name and Version, which no longer carry the platform, so a net8.0-windows job would
        // otherwise deduplicate against a plain net8.0 one. Compared as given, like everywhere else the platform is
        // used: "net8.0-Windows" and "net8.0-windows" are two runtimes, and deduplicating them is the caller's call.
        public override bool Equals(object? obj)
            => base.Equals(obj) && platform == ((CoreRuntime) obj!).platform;

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), platform);

        /// <summary>Appends the target platform, when there is one, so platform-specific jobs are distinguishable.</summary>
        public override string ToString() => IsPlatformSpecific ? $"{base.ToString()} ({platform})" : base.ToString();

        /// <summary>
        /// Whether the string is shaped like a target platform: a name, optionally followed by a version
        /// ("windows", "windows10.0.19041.0").
        /// </summary>
        /// <remarks>
        /// It ends up verbatim in the generated project's TargetFrameworks, where a stray '&lt;' would produce
        /// malformed XML and a ';' a second target framework. The shape is checked rather than the value matched
        /// against known platforms, which would need updating for every new one.
        /// </remarks>
        internal static bool IsValidPlatform(string platform)
        {
            int index = 0;
            while (index < platform.Length && char.IsLetter(platform[index]))
                index++;

            if (index == 0) // has to start with a platform name
                return false;

            // Optionally followed by a version. Every dot has to separate two digits - a leading, trailing or doubled
            // one produces a moniker MSBuild rejects.
            for (; index < platform.Length; index++)
            {
                if (platform[index] == '.')
                {
                    bool separatesDigits = index > 0 && char.IsDigit(platform[index - 1])
                        && index + 1 < platform.Length && char.IsDigit(platform[index + 1]);
                    if (!separatesDigits)
                        return false;
                }
                else if (!char.IsDigit(platform[index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns a runtime for the given version and optional platform.</summary>
        /// <exception cref="ArgumentException">
        /// The platform is not shaped like a target platform identifier, or the version predates platform-specific
        /// monikers, which would leave the platform in the runtime but absent from the moniker built from it.
        /// </exception>
        public static CoreRuntime From(Version version, string? platform = null)
            => platform.IsNotBlank()
                ? version.Major >= 5 && IsValidPlatform(platform!)
                    ? new CoreRuntime(version, platform)
                    : throw new ArgumentException(
                        $"'{platform}' is not a valid target platform for .NET {version.Major}.{version.Minor}. It has to be a platform name, optionally followed by a version, for example \"windows\" or \"windows10.0.19041.0\", and only .NET 5.0 and later have platform-specific target frameworks.",
                        nameof(platform))
                : (version.Major, version.Minor) switch
                {
                    (2, 0) => Core20,
                    (2, 1) => Core21,
                    (2, 2) => Core22,
                    (3, 0) => Core30,
                    (3, 1) => Core31,
                    (5, 0) => Core50,
                    (6, 0) => Core60,
                    (7, 0) => Core70,
                    (8, 0) => Core80,
                    (9, 0) => Core90,
                    (10, 0) => Core10_0,
                    (11, 0) => Core11_0,
                    _ => new CoreRuntime(version),
                };

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

            if (!TryGetVersion(out var version))
            {
                throw new NotSupportedException("Unable to recognize .NET Core version, please report a bug at https://github.com/dotnet/BenchmarkDotNet");
            }

            return FromVersion(version, Assembly.GetEntryAssembly());
        }

        private static CoreRuntime FromVersion(Version version, Assembly? assembly)
            => (version.Major, version.Minor) switch
            {
                (2, 0) => Core20,
                (2, 1) => Core21,
                (2, 2) => Core22,
                (3, 0) => Core30,
                (3, 1) => Core31,
                _ => GetPlatformSpecific(version, assembly),
            };

        internal static bool TryGetVersion([NotNullWhen(true)] out Version? version)
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

            // Single-file publish and NativeAot app have empty assembly location.
            string coreclrLocation = RuntimeInformation.GetCoreLibDllLocation();
            if (coreclrLocation.IsNotBlank())
            {
                var systemPrivateCoreLib = FileVersionInfo.GetVersionInfo(coreclrLocation);
                var productVersion = systemPrivateCoreLib?.ProductVersion ?? "";
                var productName = systemPrivateCoreLib?.ProductName ?? "";

                // systemPrivateCoreLib.Product*Part properties return 0 so we have to implement some ugly parsing...
                if (TryGetVersionFromProductInfo(productVersion, productName, out version))
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
            string frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? "";
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
            return new([.. frameworkDescription.SkipWhile(c => !char.IsDigit(c))]);
        }

        // sample input:
        // for dotnet run: C:\Program Files\dotnet\shared\Microsoft.NETCore.App\2.1.12\
        // for dotnet publish: C:\Users\adsitnik\source\repos\ConsoleApp25\ConsoleApp25\bin\Release\netcoreapp2.0\win-x64\publish\
        internal static bool TryGetVersionFromRuntimeDirectory(string runtimeDirectory, [NotNullWhen(true)] out Version? version)
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
        internal static bool TryGetVersionFromProductInfo(string productVersion, string productName, [NotNullWhen(true)] out Version? version)
        {
            if (productVersion.IsNotBlank() && productName.IsNotBlank())
            {
                if (productName.Contains(".NET Core", StringComparison.OrdinalIgnoreCase))
                {
                    string parsableVersion = GetParsableVersionPart(productVersion);
                    if (Version.TryParse(productVersion, out version) || Version.TryParse(parsableVersion, out version))
                    {
                        return true;
                    }
                }

                // yes, .NET Core 2.X has a product name == .NET Framework...
                if (productName.Contains(".NET Framework", StringComparison.OrdinalIgnoreCase))
                {
                    const string releaseVersionPrefix = "release/";
                    int releaseVersionIndex = productVersion.IndexOf(releaseVersionPrefix, StringComparison.Ordinal);
                    if (releaseVersionIndex > 0)
                    {
                        string releaseVersion = GetParsableVersionPart(productVersion[(releaseVersionIndex + releaseVersionPrefix.Length)..]);

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
        internal static bool TryGetVersionFromFrameworkName(string frameworkName, [NotNullWhen(true)] out Version? version)
        {
            const string versionPrefix = ".NETCoreApp,Version=v";
            if (frameworkName.IsNotBlank() && frameworkName.StartsWith(versionPrefix))
            {
                string frameworkVersion = GetParsableVersionPart(frameworkName[versionPrefix.Length..]);

                return Version.TryParse(frameworkVersion, out version);
            }

            version = null;
            return false;
        }

        // Version.TryParse does not handle thing like 3.0.0-WORD
        internal static string GetParsableVersionPart(string fullVersionName) => new([.. fullVersionName.TakeWhile(c => char.IsDigit(c) || c == '.')]);

        private static CoreRuntime GetPlatformSpecific(Version version, Assembly? assembly)
            => TryGetTargetPlatform(assembly, out var platform)
                ? From(version, platform)
                : version.Major switch
                {
                    5 => Core50,
                    6 => Core60,
                    7 => Core70,
                    8 => Core80,
                    9 => Core90,
                    10 => Core10_0,
                    11 => Core11_0,
                    _ => new(version),
                };

        private static bool TryGetTargetPlatform(Assembly? assembly, [NotNullWhen(true)] out string? platform)
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

            platform = platformNameProperty.GetValue(attributeInstance) as string;

            // Read off the entry assembly, so it is not ours to trust. A malformed value is treated as no platform
            // rather than left to throw out of From(): this runs inside a static initializer, where it would surface
            // as a TypeInitializationException.
            if (platform.IsBlank() || !IsValidPlatform(platform!))
            {
                platform = null;
                return false;
            }

            return true;
        }

        public override IToolchain GetDefaultToolchain(BenchmarkCase benchmarkCase)
        {
            if (benchmarkCase.Descriptor.Type.Assembly.IsLinqPad())
                return InProcessEmitToolchain.Default;

            return CsProjCoreToolchain.From(this, NetCoreAppSettings.Default);
        }
    }
}
