using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.PlatformAbstractions;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class CustomCoreClrToolchain : Toolchain
    {
        internal CustomCoreClrToolchain(string displayName, string coreClrVersion, string coreFxVersion, string runtimeFrameworkVersion,
            string targetFrameworkMoniker, string runtimeIdentifier, 
            string customDotNetCliPath, 
            Dictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore)
            : base(displayName,
                new Generator(coreClrVersion, coreFxVersion, runtimeFrameworkVersion, targetFrameworkMoniker, runtimeIdentifier, feeds, useNuGetClearTag, useTempFolderForRestore),
                new Publisher(customDotNetCliPath),
                new Executor())
        {
        }

        public static CustomCoreClrToolchainBuilder CreateBuilder() => CustomCoreClrToolchainBuilder.Create();
    }

    public class CustomCoreClrToolchainBuilder
    {
        public static CustomCoreClrToolchainBuilder Create() => new CustomCoreClrToolchainBuilder();

        private string coreClrVersion, coreFxVersion;
        private string runtimeIdentifier, customDotNetCliPath;
        private string targetFrameworkMoniker = "netcoreapp2.1", displayName = "not set";
        private string runtimeFrameworkVersion;

        private bool isCoreClrConfigured = false, isCoreFxConfigured = false;
        private bool useNuGetClearTag = false, useTempFolderForRestore = false;

        private Dictionary<string, string> feeds = new Dictionary<string, string>();

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references local CoreClr build
        /// as described here https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md
        /// </summary>
        /// <param name="coreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="binPackagesPath">path to folder with CoreClr NuGet packages. Example: "C:\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg"</param>
        /// <param name="packagesPath">path to folder with NuGet packages restored for CoreClr build. Example: "C:\Projects\coreclr\packages"</param>
        public CustomCoreClrToolchainBuilder UseCoreClrLocalBuild(string coreClrVersion, string binPackagesPath, string packagesPath)
        {
            if (coreClrVersion == null) throw new ArgumentNullException(nameof(coreClrVersion));
            if (binPackagesPath == null) throw new ArgumentNullException(nameof(binPackagesPath));
            if (!Directory.Exists(binPackagesPath)) throw new DirectoryNotFoundException($"{binPackagesPath} does not exist");
            if (packagesPath == null) throw new ArgumentNullException(nameof(packagesPath));
            if (!Directory.Exists(packagesPath)) throw new DirectoryNotFoundException($"{packagesPath} does not exist");

            this.coreClrVersion = coreClrVersion;

            feeds[Generator.LocalCoreClrPackagesBin] = binPackagesPath;
            feeds[Generator.LocalCoreClrPackages] = packagesPath;

            isCoreClrConfigured = true;
            useNuGetClearTag = true;
            useTempFolderForRestore = true;

            return this;
        }

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references NuGet CoreClr package
        /// </summary>
        /// <param name="coreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="nugetFeedUrl">url to NuGet CoreCLR feed, The default is: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"</param>
        public CustomCoreClrToolchainBuilder UseCoreClrNuGet(string coreClrVersion, string nugetFeedUrl = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")
        {
            if (coreClrVersion == null) throw new ArgumentNullException(nameof(coreClrVersion));
            if (nugetFeedUrl == null) throw new ArgumentNullException(nameof(nugetFeedUrl));

            this.coreClrVersion = coreClrVersion;

            feeds[Generator.CoreClrNuGetFeed] = nugetFeedUrl;

            isCoreClrConfigured = true;

            return this;
        }

        /// <summary>
        /// tells the toolchain to use the default CoreClr (for given dotnet cli and moniker), emits no direct dependency to NETCore.Runtime.CoreCLR package
        /// </summary>
        public CustomCoreClrToolchainBuilder UseCoreClrDefault()
        {
            isCoreClrConfigured = true;

            return this;
        }

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references local CoreFx build
        /// as described here https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#more-advanced-scenario---using-your-local-corefx-build
        /// </summary>
        /// <param name="privateCoreFxNetCoreAppVersion">the version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: "4.5.0-preview2-26307-0"</param>
        /// <param name="binPackagesPath">path to folder with CoreFX NuGet packages, Example: "C:\Projects\forks\corefx\bin\packages\Release"</param>
        public CustomCoreClrToolchainBuilder UseCoreFxLocalBuild(string privateCoreFxNetCoreAppVersion, string binPackagesPath)
        {
            if (privateCoreFxNetCoreAppVersion == null) throw new ArgumentNullException(nameof(privateCoreFxNetCoreAppVersion));
            if (binPackagesPath == null) throw new ArgumentNullException(nameof(binPackagesPath));
            if (!Directory.Exists(binPackagesPath)) throw new DirectoryNotFoundException($"{binPackagesPath} does not exist");

            this.coreFxVersion = privateCoreFxNetCoreAppVersion;
            feeds[Generator.LocalCoreFxPacakgesBin] = binPackagesPath;
            isCoreFxConfigured = true;
            useTempFolderForRestore = true;

            return this;
        }

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references NuGet CoreFx build
        /// </summary>
        /// <param name="privateCoreFxNetCoreAppVersion">the version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: "4.5.0-preview2-26307-0"</param>
        /// <param name="nugetFeedUrl">ulr to NuGet CoreFX feed, The default is: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"</param>
        public CustomCoreClrToolchainBuilder UseCoreFxNuGet(string privateCoreFxNetCoreAppVersion, string nugetFeedUrl = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")
        {
            if (privateCoreFxNetCoreAppVersion == null) throw new ArgumentNullException(nameof(privateCoreFxNetCoreAppVersion));
            if (nugetFeedUrl == null) throw new ArgumentNullException(nameof(nugetFeedUrl));

            coreFxVersion = privateCoreFxNetCoreAppVersion;
            feeds[Generator.CoreFxNuGetFeed] = nugetFeedUrl;
            isCoreFxConfigured = true;

            return this;
        }

        /// <summary>
        /// tells the toolchain to use the default CoreFx (for given dotnet cli and moniker), emits no direct dependency to NetCore.App package
        /// </summary>
        public CustomCoreClrToolchainBuilder UseCoreFxDefault()
        {
            isCoreFxConfigured = true;

            return this;
        }

        /// <summary>
        /// emits clear tag in the auto-generated NuGet.config file, by default true for local builds
        /// </summary>
        public CustomCoreClrToolchainBuilder UseNuGetClearTag(bool value)
        {
            useNuGetClearTag = value;

            return this;
        }

        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        public CustomCoreClrToolchainBuilder TargetFrameworkMoniker(string targetFrameworkMoniker = "netcoreapp2.1")
        {
            this.targetFrameworkMoniker = targetFrameworkMoniker ?? throw new ArgumentNullException(nameof(targetFrameworkMoniker));

            return this;
        }

        /// <param name="customDotNetCliPath">if not provided, the one from PATH will be used</param>
        public CustomCoreClrToolchainBuilder DotNetCli(string customDotNetCliPath)
        {
            if (!string.IsNullOrEmpty(customDotNetCliPath) && !File.Exists(customDotNetCliPath))
                throw new FileNotFoundException("Given file does not exist", customDotNetCliPath);

            this.customDotNetCliPath = customDotNetCliPath;

            return this;
        }

        /// <param name="runtimeIdentifier">if not provided, portable OS-arch will be used (example: "win-x64", "linux-x86")</param>
        public CustomCoreClrToolchainBuilder RuntimeIdentifier(string runtimeIdentifier)
        {
            this.runtimeIdentifier = runtimeIdentifier;

            return this;
        }

        /// <param name="runtimeFrameworkVersion">optional, when set it's copied to the generated .csproj file</param>
        public CustomCoreClrToolchainBuilder RuntimeFrameworkVersion(string runtimeFrameworkVersion)
        {
            this.runtimeFrameworkVersion = runtimeFrameworkVersion;

            return this;
        }

        /// <param name="displayName">the name of the toolchain to be displayed in results</param>
        public CustomCoreClrToolchainBuilder DisplayName(string displayName)
        {
            if (String.IsNullOrEmpty(displayName)) throw new ArgumentException("Value cannot be null or empty.", nameof(displayName));

            this.displayName = displayName;

            return this;
        }

        /// <summary>
        /// restore to temp folder to keep your CI clean or install same package many times (perhaps with different content but same version number), by default true for local builds
        /// https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md#3---consuming-subsequent-code-changes-by-rebuilding-the-package-alternative-2
        /// </summary>
        public CustomCoreClrToolchainBuilder UseTempFolderForRestore(bool value)
        {
            useTempFolderForRestore = value;

            return this;
        }

        public IToolchain ToToolchain()
        {
            if (!isCoreClrConfigured)
                throw new InvalidOperationException("You need to use one of the UseCoreClr* methods to tell us which CoreClr to use.");

            if (!isCoreFxConfigured)
                throw new InvalidOperationException("You need to use one of the UseCoreFx* methods to tell us which CoreFx to use.");

            return new CustomCoreClrToolchain(
                displayName: displayName,
                coreClrVersion: coreClrVersion,
                coreFxVersion: coreFxVersion,
                runtimeFrameworkVersion: runtimeFrameworkVersion,
                targetFrameworkMoniker: targetFrameworkMoniker,
                runtimeIdentifier: runtimeIdentifier ?? GetPortableRuntimeIdentifier(),
                customDotNetCliPath: customDotNetCliPath,
                feeds: feeds,
                useNuGetClearTag: useNuGetClearTag,
                useTempFolderForRestore: useTempFolderForRestore);
        }

        private static string GetPortableRuntimeIdentifier()
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
