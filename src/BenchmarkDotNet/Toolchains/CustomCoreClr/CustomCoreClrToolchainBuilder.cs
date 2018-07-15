using System;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class CustomCoreClrToolchainBuilder : CustomDotNetCliToolchainBuilder
    {
        public static CustomCoreClrToolchainBuilder Create() => new CustomCoreClrToolchainBuilder();

        private string coreClrVersion, coreFxVersion;
        private bool isCoreClrConfigured = false, isCoreFxConfigured = false;

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
            feeds[Generator.LocalCoreFxPackagesBin] = binPackagesPath;
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

        public override IToolchain ToToolchain()
        {
            if (!isCoreClrConfigured)
                throw new InvalidOperationException("You need to use one of the UseCoreClr* methods to tell us which CoreClr to use.");

            if (!isCoreFxConfigured)
                throw new InvalidOperationException("You need to use one of the UseCoreFx* methods to tell us which CoreFx to use.");

            return new CustomCoreClrToolchain(
                displayName: displayName ?? "custom .NET Core",
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
    }
}