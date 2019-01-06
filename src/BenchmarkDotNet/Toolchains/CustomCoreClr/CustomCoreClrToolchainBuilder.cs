﻿using System;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class CustomCoreClrToolchainBuilder : CustomDotNetCliToolchainBuilder
    {
        public static CustomCoreClrToolchainBuilder Create() => new CustomCoreClrToolchainBuilder();

        private string coreClrVersion, coreFxVersion;
        private bool isCoreClrConfigured, isCoreFxConfigured;

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references local CoreClr build
        /// as described here https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md
        /// </summary>
        /// <param name="newCoreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="binPackagesPath">path to folder with CoreClr NuGet packages. Example: "C:\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg"</param>
        /// <param name="packagesPath">path to folder with NuGet packages restored for CoreClr build. Example: "C:\Projects\coreclr\packages"</param>
        public CustomCoreClrToolchainBuilder UseCoreClrLocalBuild(string newCoreClrVersion, string binPackagesPath, string packagesPath)
        {
            if (binPackagesPath == null) throw new ArgumentNullException(nameof(binPackagesPath));
            if (!Directory.Exists(binPackagesPath)) throw new DirectoryNotFoundException($"{binPackagesPath} does not exist");
            if (packagesPath == null) throw new ArgumentNullException(nameof(packagesPath));
            if (!Directory.Exists(packagesPath)) throw new DirectoryNotFoundException($"{packagesPath} does not exist");

            coreClrVersion = newCoreClrVersion ?? throw new ArgumentNullException(nameof(newCoreClrVersion));

            Feeds[Generator.LocalCoreClrPackagesBin] = binPackagesPath;
            Feeds[Generator.LocalCoreClrPackages] = packagesPath;

            isCoreClrConfigured = true;
            useTempFolderForRestore = true;

            return this;
        }

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references NuGet CoreClr package
        /// </summary>
        /// <param name="newCoreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="nuGetFeedUrl">url to NuGet CoreCLR feed, The default is: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"</param>
        public CustomCoreClrToolchainBuilder UseCoreClrNuGet(string newCoreClrVersion, string nuGetFeedUrl = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")
        {
            coreClrVersion = newCoreClrVersion ?? throw new ArgumentNullException(nameof(newCoreClrVersion));

            Feeds[Generator.CoreClrNuGetFeed] = nuGetFeedUrl ?? throw new ArgumentNullException(nameof(nuGetFeedUrl));

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
            if (binPackagesPath == null) throw new ArgumentNullException(nameof(binPackagesPath));
            if (!Directory.Exists(binPackagesPath)) throw new DirectoryNotFoundException($"{binPackagesPath} does not exist");

            coreFxVersion = privateCoreFxNetCoreAppVersion ?? throw new ArgumentNullException(nameof(privateCoreFxNetCoreAppVersion));
            Feeds[Generator.LocalCoreFxPackagesBin] = binPackagesPath;
            isCoreFxConfigured = true;
            useTempFolderForRestore = true;

            return this;
        }

        /// <summary>
        /// creates a toolchain which publishes self-contained app which references NuGet CoreFx build
        /// </summary>
        /// <param name="privateCoreFxNetCoreAppVersion">the version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: "4.5.0-preview2-26307-0"</param>
        /// <param name="nuGetFeedUrl">url to NuGet CoreFX feed, The default is: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"</param>
        public CustomCoreClrToolchainBuilder UseCoreFxNuGet(string privateCoreFxNetCoreAppVersion, string nuGetFeedUrl = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")
        {
            coreFxVersion = privateCoreFxNetCoreAppVersion ?? throw new ArgumentNullException(nameof(privateCoreFxNetCoreAppVersion));
            Feeds[Generator.CoreFxNuGetFeed] = nuGetFeedUrl ?? throw new ArgumentNullException(nameof(nuGetFeedUrl));
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
                targetFrameworkMoniker: GetTargetFrameworkMoniker(),
                runtimeIdentifier: runtimeIdentifier ?? GetPortableRuntimeIdentifier(),
                customDotNetCliPath: customDotNetCliPath,
                feeds: Feeds,
                useNuGetClearTag: useNuGetClearTag,
                useTempFolderForRestore: useTempFolderForRestore,
                timeout: timeout ?? TimeSpan.FromMinutes(3)); // downloading entire Runtime for this toolchain might take a while!
        }
    }
}