using System;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Jobs
{
    public class NuGetReference : IEquatable<NuGetReference>
    {
        public NuGetReference(string packageName, string packageVersion, Uri? source = null, bool prerelease = false)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("message", nameof(packageName));

            PackageName = packageName;

            if (!string.IsNullOrWhiteSpace(packageVersion) && !IsValidVersion(packageVersion))
                throw new InvalidOperationException($"Invalid version specified: {packageVersion}");

            PackageVersion = packageVersion ?? string.Empty;
            PackageSource = source;
            Prerelease = prerelease;
        }

        public string PackageName { get; }
        public string PackageVersion { get; }
        public bool Prerelease { get; }
        public Uri PackageSource { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as NuGetReference);
        }

        /// <summary>
        /// Object is equals when the package name and version are the same
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(NuGetReference other)
        {
            return other != null &&
                   PackageName == other.PackageName &&
                   PackageVersion == other.PackageVersion;
        }

        public override int GetHashCode() => HashCode.Combine(PackageName, PackageVersion);

        public override string ToString() => $"{PackageName}{(string.IsNullOrWhiteSpace(PackageVersion) ? string.Empty : $" {PackageVersion}")}";

        /// <summary>
        /// Tries to validate the version string
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        private bool IsValidVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return false;
            //There is a great NuGet package for semver validation called `semver` however we probably
            // don't want to add another dependency here so this will do some rudimentary validation
            // and if that fails, then the actual add package command will fail anyways.
            var parts = version.Split('-');
            if (parts.Length == 0) return false;
            if (!Version.TryParse(parts[0], out var _)) return false;
            for (int i = 1; i < parts.Length; i++)
            {
                if (!PreReleaseValidator.IsMatch(parts[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// Used to validate all pre-release parts of a semver version
        /// </summary>
        /// <remarks>
        /// Allows alphanumeric chars, ".", "+", "-"
        /// </remarks>
        private static readonly Regex PreReleaseValidator = new Regex(@"^[0-9A-Za-z\-\+\.]+$", RegexOptions.Compiled);
    }
}