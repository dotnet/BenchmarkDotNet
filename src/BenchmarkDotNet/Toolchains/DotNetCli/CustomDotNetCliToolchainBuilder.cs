﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Microsoft.DotNet.PlatformAbstractions;

namespace BenchmarkDotNet.Toolchains.DotNetCli
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class CustomDotNetCliToolchainBuilder
    {
        protected readonly Dictionary<string, string> Feeds = new Dictionary<string, string>();

        protected string runtimeIdentifier, customDotNetCliPath;
        protected string targetFrameworkMoniker = "netcoreapp2.1", displayName;
        protected string runtimeFrameworkVersion;

        protected bool useNuGetClearTag, useTempFolderForRestore;
        protected TimeSpan? timeout;

        public abstract IToolchain ToToolchain();

        /// <summary>it allows you to define an additional NuGet feed, you can seal the feeds list by using the UseNuGetClearTag() method</summary>
        /// <param name="feedName">the name of the feed, will be used in the auto-generated NuGet.config file</param>
        /// <param name="feedAddress">the address of the feed, will be used in the auto-generated NuGet.config file</param>
        [PublicAPI]
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

        /// <param name="newTargetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder TargetFrameworkMoniker(string newTargetFrameworkMoniker = "netcoreapp2.1")
        {
            targetFrameworkMoniker = newTargetFrameworkMoniker ?? throw new ArgumentNullException(nameof(newTargetFrameworkMoniker));

            return this;
        }

        /// <param name="newCustomDotNetCliPath">if not provided, the one from PATH will be used</param>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder DotNetCli(string newCustomDotNetCliPath)
        {
            if (!string.IsNullOrEmpty(newCustomDotNetCliPath) && !File.Exists(newCustomDotNetCliPath))
                throw new FileNotFoundException("Given file does not exist", newCustomDotNetCliPath);

            customDotNetCliPath = newCustomDotNetCliPath;

            return this;
        }

        /// <param name="newRuntimeIdentifier">if not provided, portable OS-arch will be used (example: "win-x64", "linux-x86")</param>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder RuntimeIdentifier(string newRuntimeIdentifier)
        {
            runtimeIdentifier = newRuntimeIdentifier;

            return this;
        }

        /// <param name="newRuntimeFrameworkVersion">optional, when set it's copied to the generated .csproj file</param>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder RuntimeFrameworkVersion(string newRuntimeFrameworkVersion)
        {
            runtimeFrameworkVersion = newRuntimeFrameworkVersion;

            return this;
        }

        /// <param name="newDisplayName">the name of the toolchain to be displayed in results</param>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder DisplayName(string newDisplayName)
        {
            if (string.IsNullOrEmpty(newDisplayName)) throw new ArgumentException("Value cannot be null or empty.", nameof(newDisplayName));

            displayName = newDisplayName;

            return this;
        }

        /// <summary>
        /// restore to temp folder to keep your CI clean or install same package many times (perhaps with different content but same version number), by default true for local builds
        /// https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#3---consuming-subsequent-code-changes-by-rebuilding-the-package-alternative-2
        /// </summary>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder UseTempFolderForRestore(bool value)
        {
            useTempFolderForRestore = value;

            return this;
        }

        /// <summary>
        /// sets provided timeout for build
        /// </summary>
        [PublicAPI]
        public CustomDotNetCliToolchainBuilder Timeout(TimeSpan timeout)
        {
            this.timeout = timeout;

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