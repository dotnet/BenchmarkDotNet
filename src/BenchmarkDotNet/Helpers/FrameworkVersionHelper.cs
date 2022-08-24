using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace BenchmarkDotNet.Helpers
{
    internal static class FrameworkVersionHelper
    {
        // magic numbers come from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        // should be ordered by release number
        private static readonly (int minReleaseNumber, string version)[] FrameworkVersions =
        {
            (533320, "4.8.1"), // value taken from Windows 11 arm64 insider build
            (528040, "4.8"),
            (461808, "4.7.2"),
            (461308, "4.7.1"),
            (460798, "4.7"),
            (394802, "4.6.2"),
            (394254, "4.6.1")
        };

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
            if (string.Compare(servicingVersion, "4.6.2") < 0)
                return "4.6.1";
            if (string.Compare(servicingVersion, "4.7") < 0)
                return "4.6.2";
            if (string.Compare(servicingVersion, "4.7.1") < 0)
                return "4.7";
            if (string.Compare(servicingVersion, "4.7.2") < 0)
                return "4.7.1";
            if (string.Compare(servicingVersion, "4.8") < 0)
                return "4.7.2";
            if (string.Compare(servicingVersion, "4.8.9") < 0)
                return "4.8";

            return "4.8.1"; // most probably the last major release of Full .NET Framework
        }


#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        private static int? GetReleaseNumberFromWindowsRegistry()
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                if (ndpKey == null)
                    return null;
                return Convert.ToInt32(ndpKey.GetValue("Release"));
            }
        }

#if NET6_0_OR_GREATER
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
#endif
        internal static string GetLatestNetDeveloperPackVersion()
        {
            if (!(GetReleaseNumberFromWindowsRegistry() is int releaseNumber))
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