using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class CustomDotNetCliToolchainBuilder
    {
        protected string runtimeIdentifier, customDotNetCliPath;
        protected string targetFrameworkMoniker = "netcoreapp2.1", displayName;
        protected string runtimeFrameworkVersion;

        protected bool useNuGetClearTag, useTempFolderForRestore;
        protected readonly Dictionary<string, string> Feeds = new Dictionary<string, string>();

        public abstract IToolchain ToToolchain();

        /// <summary>it allows you to define an additional NuGet feed, you can seal the feeds list by using the UseNuGetClearTag() method</summary>
        /// <param name="feedName">the name of the feed, will be used in the auto-generated NuGet.config file</param>
        /// <param name="feedAddress">the address of the feed, will be used in the auto-generated NuGet.config file</param>
        public CustomDotNetCliToolchainBuilder AdditionalNuGetFeed(string feedName, string feedAddress)
        {
            if (string.IsNullOrEmpty(feedName)) throw new ArgumentException("Value cannot be null or empty.", nameof(feedName));
            if (string.IsNullOrEmpty(feedAddress)) throw new ArgumentException("Value cannot be null or empty.", nameof(feedAddress));

            Feeds[feedName] = feedAddress;

            return this;
        }

        /// <summary>
        /// emits clear tag in the auto-generated NuGet.config file
        /// </summary>
        public CustomDotNetCliToolchainBuilder UseNuGetClearTag(bool value)
        {
            useNuGetClearTag = value;

            return this;
        }

        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        public CustomDotNetCliToolchainBuilder TargetFrameworkMoniker(string targetFrameworkMoniker = "netcoreapp2.1")
        {
            this.targetFrameworkMoniker = targetFrameworkMoniker ?? throw new ArgumentNullException(nameof(targetFrameworkMoniker));

            return this;
        }

        /// <param name="customDotNetCliPath">if not provided, the one from PATH will be used</param>
        public CustomDotNetCliToolchainBuilder DotNetCli(string customDotNetCliPath)
        {
            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
                throw new FileNotFoundException("Given file does not exist", customDotNetCliPath);

            this.customDotNetCliPath = customDotNetCliPath;

            return this;
        }

        /// <param name="runtimeIdentifier">if not provided, portable OS-arch will be used (example: "win-x64", "linux-x86")</param>
        public CustomDotNetCliToolchainBuilder RuntimeIdentifier(string runtimeIdentifier)
        {
            this.runtimeIdentifier = runtimeIdentifier;

            return this;
        }

        /// <param name="runtimeFrameworkVersion">optional, when set it's copied to the generated .csproj file</param>
        public CustomDotNetCliToolchainBuilder RuntimeFrameworkVersion(string runtimeFrameworkVersion)
        {
            this.runtimeFrameworkVersion = runtimeFrameworkVersion;

            return this;
        }

        /// <param name="displayName">the name of the toolchain to be displayed in results</param>
        public CustomDotNetCliToolchainBuilder DisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) throw new ArgumentException("Value cannot be null or empty.", nameof(displayName));

            this.displayName = displayName;

            return this;
        }

        /// <summary>
        /// restore to temp folder to keep your CI clean or install same package many times (perhaps with different content but same version number), by default true for local builds
        /// https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#3---consuming-subsequent-code-changes-by-rebuilding-the-package-alternative-2
        /// </summary>
        public CustomDotNetCliToolchainBuilder UseTempFolderForRestore(bool value)
        {
            useTempFolderForRestore = value;

            return this;
        }

        protected static string GetPortableRuntimeIdentifier()
        {
            // Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()
            // returns win10-x64, we want the simpler form win-x64
            // the values taken from https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#macos-rids
            string osPart = RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows
                ? "win" : (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux ? "linux" : "osx");

            return $"{osPart}-{RuntimeEnvironment.RuntimeArchitecture}";
        }
    }
}