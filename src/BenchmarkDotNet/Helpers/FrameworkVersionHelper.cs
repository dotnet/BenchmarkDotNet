using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Helpers
{
    internal static class FrameworkVersionHelper
    {
        // magic numbers come from https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        // should be ordered by release number
        private static readonly (int minReleaseNumber, string version)[] FrameworkVersions =
        {
            (461808, "4.7.2"),
            (461308, "4.7.1"),
            (460798, "4.7"),
            (394802, "4.6.2"),
            (394254, "4.6.1")
        };
        
        private static int? GetReleaseNumberFromWindowsRegistry()
        {
            using (var ndpKey = Microsoft.Win32.RegistryKey
                .OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                if (ndpKey == null)
                    return null;
                return Convert.ToInt32(ndpKey.GetValue("Release"));
            }
        }
        
        [NotNull]
        internal static string GetCurrentNetFrameworkVersion()
        {
            var releaseNumber = GetReleaseNumberFromWindowsRegistry();
            if (!releaseNumber.HasValue)
                return "?";

            return FrameworkVersions
                       .FirstOrDefault(v => releaseNumber >= v.minReleaseNumber)
                       .version ?? "?";
        }

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