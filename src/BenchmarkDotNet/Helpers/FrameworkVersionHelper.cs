using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace BenchmarkDotNet.Helpers
{
    internal static class FrameworkVersionHelper
    {
        // magic numbers come from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        // should be ordered by release number
        private static readonly (int minReleaseNumber, string version)[] FrameworkVersions =
        [
            (533320, "4.8.1"), // value taken from Windows 11 arm64 insider build
            (528040, "4.8"),
            (461808, "4.7.2"),
            (461308, "4.7.1"),
            (460798, "4.7"),
            (394802, "4.6.2"),
            (394254, "4.6.1")
        ];

        internal static string? GetTargetFrameworkVersion(Assembly? assembly)
            // Look for a TargetFrameworkAttribute with a supported Framework version.
            => assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName switch
            {
                ".NETFramework,Version=v4.6.1" => "4.6.1",
                ".NETFramework,Version=v4.6.2" => "4.6.2",
                ".NETFramework,Version=v4.7" => "4.7",
                ".NETFramework,Version=v4.7.1" => "4.7.1",
                ".NETFramework,Version=v4.7.2" => "4.7.2",
                ".NETFramework,Version=v4.8" => "4.8",
                ".NETFramework,Version=v4.8.1" => "4.8.1",
                // Null assembly, or TargetFrameworkAttribute not found, or the assembly targeted a version older than we support,
                // or the assembly targeted a non-framework tfm (like netstandard2.0).
                _ => null,
            };

        internal static Version? GetTargetCoreVersion(Assembly? assembly)
        {
            //.NETCoreApp,Version=vX.Y
            const string FrameworkPrefix = ".NETCoreApp,Version=v";

            // Look for a TargetFrameworkAttribute with a supported Framework version.
            string? framework = assembly?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            if (framework?.StartsWith(FrameworkPrefix) == true
                && Version.TryParse(framework[FrameworkPrefix.Length..], out var version)
                // We don't support netcoreapp1.X
                && version.Major >= 2)
            {
                return version;
            }

            // Null assembly, or TargetFrameworkAttribute not found, or the assembly targeted a version older than we support,
            // or the assembly targeted a non-core tfm (like netstandard2.0).
            return null;
        }

        internal static string GetFrameworkDescription()
        {
            var fullName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription; // sth like .NET Framework 4.7.3324.0
            var servicingVersion = new string(fullName.SkipWhile(c => !char.IsDigit(c)).ToArray());
            var releaseVersion = MapToReleaseVersion(servicingVersion);

            return $".NET Framework {releaseVersion} ({servicingVersion})";
        }

        internal static string GetFrameworkReleaseVersion()
        {
            var fullName = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription; // sth like .NET Framework 4.7.3324.0
            var servicingVersion = new string(fullName.SkipWhile(c => !char.IsDigit(c)).ToArray());
            return MapToReleaseVersion(servicingVersion);
        }

        internal static string MapToReleaseVersion(string servicingVersion)
        {
            // the following code assumes that .NET 4.6.1 is the oldest supported version
            if (string.CompareOrdinal(servicingVersion, "4.6.2") < 0)
                return "4.6.1";
            if (string.CompareOrdinal(servicingVersion, "4.7") < 0)
                return "4.6.2";
            if (string.CompareOrdinal(servicingVersion, "4.7.1") < 0)
                return "4.7";
            if (string.CompareOrdinal(servicingVersion, "4.7.2") < 0)
                return "4.7.1";
            if (string.CompareOrdinal(servicingVersion, "4.8") < 0)
                return "4.7.2";
            if (string.CompareOrdinal(servicingVersion, "4.8.9") < 0)
                return "4.8";

            return "4.8.1"; // most probably the last major release of Full .NET Framework
        }

        [SupportedOSPlatform("windows")]
        private static int? GetReleaseNumberFromWindowsRegistry()
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
            if (ndpKey == null)
                return null;
            return Convert.ToInt32(ndpKey.GetValue("Release"));
        }

        [SupportedOSPlatform("windows")]
        internal static string? GetLatestNetDeveloperPackVersion()
        {
            if (GetReleaseNumberFromWindowsRegistry() is not int releaseNumber)
                return null;

            return FrameworkVersions
                .FirstOrDefault(v => releaseNumber >= v.minReleaseNumber && IsDeveloperPackInstalled(v.version))
                .version;
        }

        // Reference Assemblies exists when Developer Pack is installed
        private static bool IsDeveloperPackInstalled(string version) => Directory.Exists(Path.Combine(
            ProgramFilesX86DirectoryPath, @"Reference Assemblies\Microsoft\Framework\.NETFramework", 'v' + version));

        private static readonly string ProgramFilesX86DirectoryPath = Environment.GetFolderPath(
            Environment.Is64BitOperatingSystem
                ? Environment.SpecialFolder.ProgramFilesX86
                : Environment.SpecialFolder.ProgramFiles);
    }
}