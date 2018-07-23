using System;

namespace BenchmarkDotNet.Jobs
{
    public class NugetReference
    {
        public NugetReference(string packageName, string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("message", nameof(packageName));

            PackageName = packageName;
            PackageVersion = packageVersion;
            if (!string.IsNullOrWhiteSpace(PackageVersion) && !Version.TryParse(PackageVersion, out var version))
                throw new InvalidOperationException($"Invalid version specified: {packageVersion}");
        }

        public string PackageName { get; }
        public string PackageVersion { get; }

        public override string ToString()
        {
            return $"{PackageName}{(string.IsNullOrWhiteSpace(PackageVersion) ? string.Empty : $" {PackageVersion}")}";
        }
    }
}