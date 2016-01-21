#if DNX451
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.PlatformAbstractions;

namespace BenchmarkDotNet.Extensions
{
    internal static class AssemblyExtensions
    {
        internal static string TryGetLocation(this Assembly assembly, string referenceName)
        {
            if (!string.IsNullOrEmpty(assembly.Location))
            {
                return assembly.Location;
            }

            return GetPossibleDirectories(referenceName)
                .Where(Directory.Exists)
                .Select(possiblePath => Path.Combine(possiblePath, assembly.ManifestModule.ScopeName))
                .FirstOrDefault(File.Exists);
        }

        private static IEnumerable<string> GetPossibleDirectories(string referenceName)
        {
            var applicationBasePath = PlatformServices.Default?.Application?.ApplicationBasePath;
            var configuration = PlatformServices.Default?.Application?.Configuration ?? "Release";

            if (!string.IsNullOrEmpty(applicationBasePath) && Directory.Exists(applicationBasePath))
            {
                yield return Path.Combine(
                    applicationBasePath,
                    $@"..\artifacts\bin\{referenceName}\{configuration}\dnx451");

                yield return Path.Combine(
                    applicationBasePath,
                    $@"..\..\artifacts\bin\{referenceName}\{configuration}\dnx451");
            }

            //yield return @"%USERPROFILE%\.dnx\packages";
        }
    }
}
#endif