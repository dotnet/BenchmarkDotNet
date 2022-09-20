using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.NativeAot
{
    public class NativeAotToolchainBuilder : CustomDotNetCliToolchainBuilder
    {
        public static NativeAotToolchainBuilder Create() => new NativeAotToolchainBuilder();

        private string ilCompilerVersion;
        private string packagesRestorePath;
        // we set those default values on purpose https://github.com/dotnet/BenchmarkDotNet/pull/1057#issuecomment-461832612
        private bool rootAllApplicationAssemblies;
        private bool ilcGenerateCompleteTypeMetadata = true;
        private bool ilcGenerateStackTraceData = true;
        private string ilcOptimizationPreference = "Speed";
        private string ilcInstructionSet = null;

        private bool isIlCompilerConfigured;

        /// <summary>
        /// creates a NativeAOT toolchain targeting NuGet build of Microsoft.DotNet.ILCompiler
        /// Based on https://github.com/dotnet/runtimelab/blob/d0a37893a67c125f9b0cd8671846ff7d867df241/samples/HelloWorld/README.md#add-corert-to-your-project
        /// </summary>
        /// <param name="microsoftDotNetILCompilerVersion">the version of Microsoft.DotNet.ILCompiler which should be used. The default is empty which maps to latest version.</param>
        /// <param name="nuGetFeedUrl">url to NuGet feed, The default is: "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json"</param>
        [PublicAPI]
        public NativeAotToolchainBuilder UseNuGet(string microsoftDotNetILCompilerVersion = "", string nuGetFeedUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json")
        {
            ilCompilerVersion = microsoftDotNetILCompilerVersion;

            Feeds[Generator.NativeAotNuGetFeed] = nuGetFeedUrl ?? throw new ArgumentNullException(nameof(nuGetFeedUrl));

            DisplayName(string.IsNullOrEmpty(ilCompilerVersion) ? "Latest ILCompiler" : $"ILCompiler {ilCompilerVersion}");

            isIlCompilerConfigured = true;

            return this;
        }

        /// <summary>
        /// creates a NativeAOT toolchain targeting local build of ILCompiler
        /// Based on https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/nativeaot.md
        /// </summary>
        /// <param name="ilcPackages">the path to shipping packages, example: "C:\runtime\artifacts\packages\Release\Shipping"</param>
        [PublicAPI]
        public NativeAotToolchainBuilder UseLocalBuild(DirectoryInfo ilcPackages)
        {
            if (ilcPackages == null) throw new ArgumentNullException(nameof(ilcPackages));
            if (!ilcPackages.Exists) throw new DirectoryNotFoundException($"{ilcPackages} provided as {nameof(ilcPackages)} does NOT exist");

            Feeds["local"] = ilcPackages.FullName;
            ilCompilerVersion = "7.0.0-dev";
            Feeds["dotnet7"] = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json";
            useTempFolderForRestore = true;
            DisplayName("local ILCompiler build");

            isIlCompilerConfigured = true;

            return this;
        }

        /// <summary>
        /// The directory to restore packages to (optional).
        /// </summary>
        [PublicAPI]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public NativeAotToolchainBuilder PackagesRestorePath(string packagesRestorePath)
        {
            this.packagesRestorePath = packagesRestorePath;

            return this;
        }

        /// <summary>
        /// This controls the compiler behavior where all code in the application assemblies is considered dynamically reachable.
        /// This option is disabled by default.
        /// Enabling this option (true) has a significant effect on the size of the resulting executable because it prevents removal of unused code that would otherwise happen.
        /// </summary>
        [PublicAPI]
        public NativeAotToolchainBuilder RootAllApplicationAssemblies(bool value)
        {
            rootAllApplicationAssemblies = value;

            return this;
        }

        /// <summary>
        /// This controls the generation of complete type metadata.
        /// This option is enabled by default.
        /// This is a compilation mode that prevents a situation where some members of a type are visible to reflection at runtime, but others aren't, because they weren't compiled.
        /// </summary>
        /// <param name="value"></param>
        [PublicAPI]
        public NativeAotToolchainBuilder IlcGenerateCompleteTypeMetadata(bool value)
        {
            ilcGenerateCompleteTypeMetadata = value;

            return this;
        }

        /// <summary>
        /// This controls generation of stack trace metadata that provides textual names in stack traces.
        /// This option is enabled by default.
        /// This is for example the text string one gets by calling Exception.ToString() on a caught exception.
        /// With this option disabled, stack traces will still be generated, but will be based on reflection metadata alone (they might be less complete).
        /// </summary>
        [PublicAPI]
        public NativeAotToolchainBuilder IlcGenerateStackTraceData(bool value)
        {
            ilcGenerateStackTraceData = value;

            return this;
        }

        /// <summary>
        /// Options related to code generation.
        /// </summary>
        /// <param name="value">"Speed" to favor code execution speed (default), "Size" to favor smaller code size</param>
        [PublicAPI]
        public NativeAotToolchainBuilder IlcOptimizationPreference(string value = "Speed")
        {
            ilcOptimizationPreference = value;

            return this;
        }

        /// <summary>
        /// By default, the compiler targets the minimum instruction set supported by the target OS and architecture.
        /// This option allows targeting newer instruction sets for better performance.
        /// The native binary will require the instruction sets to be supported by the hardware in order to run.
        /// For example, `avx2,bmi2,fma,pclmul,popcnt,aes` will produce binary that takes advantage of instruction sets
        /// that are typically present on current Intel and AMD processors.
        /// </summary>
        /// <param name="value">Specify empty string ("", not null) to use the defaults.</param>
        [PublicAPI]
        public NativeAotToolchainBuilder IlcInstructionSet(string value)
        {
            ilcInstructionSet = value;

            return this;
        }

        [PublicAPI]
        public override IToolchain ToToolchain()
        {
            if (!isIlCompilerConfigured)
                throw new InvalidOperationException("You need to use UseNuGet or UseLocalBuild methods to tell us which ILCompiler to use.");

            return new NativeAotToolchain(
                displayName: displayName,
                ilCompilerVersion: ilCompilerVersion,
                runtimeFrameworkVersion: runtimeFrameworkVersion,
                targetFrameworkMoniker: GetTargetFrameworkMoniker(),
                runtimeIdentifier: runtimeIdentifier ?? GetPortableRuntimeIdentifier(),
                customDotNetCliPath: customDotNetCliPath,
                packagesRestorePath: packagesRestorePath,
                feeds: Feeds,
                useNuGetClearTag: useNuGetClearTag,
                useTempFolderForRestore: useTempFolderForRestore,
                rootAllApplicationAssemblies: rootAllApplicationAssemblies,
                ilcGenerateCompleteTypeMetadata: ilcGenerateCompleteTypeMetadata,
                ilcGenerateStackTraceData: ilcGenerateStackTraceData,
                ilcOptimizationPreference: ilcOptimizationPreference,
                ilcInstructionSet: ilcInstructionSet
            );
        }
    }
}
