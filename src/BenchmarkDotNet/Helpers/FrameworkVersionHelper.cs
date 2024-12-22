using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        internal static string? GetTargetFrameworkVersion()
        {
            // Search assemblies until we find a TargetFrameworkAttribute with a supported Framework version.
            // We don't search all assemblies, only the entry assembly and callers.
            foreach (var assembly in EnumerateAssemblies())
            {
                foreach (var attribute in assembly.GetCustomAttributes<TargetFrameworkAttribute>())
                {
                    switch (attribute.FrameworkName)
                    {
                        case ".NETFramework,Version=v4.6.1": return "4.6.1";
                        case ".NETFramework,Version=v4.6.2": return "4.6.2";
                        case ".NETFramework,Version=v4.7": return "4.7";
                        case ".NETFramework,Version=v4.7.1": return "4.7.1";
                        case ".NETFramework,Version=v4.7.2": return "4.7.2";
                        case ".NETFramework,Version=v4.8": return "4.8";
                        case ".NETFramework,Version=v4.8.1": return "4.8.1";
                    }
                }
            }

            return null;

            static IEnumerable<Assembly> EnumerateAssemblies()
            {
                var entryAssembly = Assembly.GetEntryAssembly();
                // Assembly.GetEntryAssembly() returns null in unit test frameworks.
                if (entryAssembly != null)
                {
                    yield return entryAssembly;
                }
                // Search calling assemblies starting from the highest stack frame
                // (expected to be the entry assembly if Assembly.GetEntryAssembly() returned null),
                // excluding this assembly.
                var stacktrace = new StackTrace(false);
                var searchedAssemblies = new HashSet<Assembly>()
                {
                    stacktrace.GetFrame(0).GetMethod().ReflectedType.Assembly
                };
                for (int i = stacktrace.FrameCount - 1; i >= 1 ; --i)
                {
                    var assembly = stacktrace.GetFrame(i).GetMethod().ReflectedType.Assembly;
                    if (searchedAssemblies.Add(assembly))
                    {
                        yield return assembly;
                    }
                }
            }
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


#if NET6_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        private static int? GetReleaseNumberFromWindowsRegistry()
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\");
            if (ndpKey == null)
                return null;
            return Convert.ToInt32(ndpKey.GetValue("Release"));
        }

#if NET6_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
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