using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    public class CoreRtToolchainBuilder : CustomDotNetCliToolchainBuilder
    {
        public static CoreRtToolchainBuilder Create() => new CoreRtToolchainBuilder();

        private string coreRtVersion;
        private string ilcPath;
        private bool useCppCodeGenerator;
        private string packagesRestorePath;
        // we set those default values on purpose https://github.com/dotnet/BenchmarkDotNet/pull/1057#issuecomment-461832612
        private bool rootAllApplicationAssemblies;
        private bool ilcGenerateCompleteTypeMetadata = true;
        private bool ilcGenerateStackTraceData = true;

        private bool isCoreRtConfigured;

        /// <summary>
        /// creates a CoreRT toolchain targeting NuGet build of CoreRT
        /// Based on https://github.com/dotnet/runtimelab/blob/d0a37893a67c125f9b0cd8671846ff7d867df241/samples/HelloWorld/README.md#add-corert-to-your-project
        /// </summary>
        /// <param name="microsoftDotNetILCompilerVersion">the version of Microsoft.DotNet.ILCompiler which should be used. The default is: "6.0.0-*"</param>
        /// <param name="nuGetFeedUrl">url to NuGet CoreRT feed, The default is: "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json"</param>
        [PublicAPI]
        public CoreRtToolchainBuilder UseCoreRtNuGet(string microsoftDotNetILCompilerVersion = "6.0.0-*", string nuGetFeedUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json")
        {
            coreRtVersion = microsoftDotNetILCompilerVersion ?? throw new ArgumentNullException(nameof(microsoftDotNetILCompilerVersion));

            Feeds[Generator.CoreRtNuGetFeed] = nuGetFeedUrl ?? throw new ArgumentNullException(nameof(nuGetFeedUrl));

            isCoreRtConfigured = true;

            return this;
        }


        /// <summary>
        /// creates a CoreRT toolchain targeting local build for CoreRT
        /// Based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built
        /// </summary>
        /// <param name="newIlcPath">the ilcPath, an example: "C:\Projects\corert\bin\Windows_NT.x64.Release"</param>
        [PublicAPI]
        public CoreRtToolchainBuilder UseCoreRtLocal(string newIlcPath)
        {
            if (newIlcPath == null) throw new ArgumentNullException(nameof(newIlcPath));
            if (!Directory.Exists(newIlcPath)) throw new DirectoryNotFoundException($"{newIlcPath} provided as {nameof(newIlcPath)} does NOT exist");

            ilcPath = newIlcPath;
            useTempFolderForRestore = true;

            isCoreRtConfigured = true;

            return this;
        }

        /// <summary>
        /// "This approach uses transpiler to convert IL to C++, and then uses platform specific C++ compiler and linker for compiling/linking the application.
        /// The transpiler is a lot less mature than the RyuJIT path. If you came here to give CoreRT a try" please don't use this option.
        /// Based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator
        /// </summary>
        [PublicAPI]
        public CoreRtToolchainBuilder UseCppCodeGenerator()
        {
            useCppCodeGenerator = true;

            return this;
        }

        /// <summary>
        /// The directory to restore packages to (optional).
        /// </summary>
        [PublicAPI]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public CoreRtToolchainBuilder PackagesRestorePath(string packagesRestorePath)
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
        public CoreRtToolchainBuilder RootAllApplicationAssemblies(bool value)
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
        public CoreRtToolchainBuilder IlcGenerateCompleteTypeMetadata(bool value)
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
        public CoreRtToolchainBuilder IlcGenerateStackTraceData(bool value)
        {
            ilcGenerateStackTraceData = value;

            return this;
        }

        [PublicAPI]
        public override IToolchain ToToolchain()
        {
            if (!isCoreRtConfigured)
                throw new InvalidOperationException("You need to use one of the UseCoreRt* methods to tell us which CoreRT to use.");

            return new CoreRtToolchain(
                displayName: displayName ?? (coreRtVersion != null ? $"Core RT {coreRtVersion}" : "local Core RT"),
                coreRtVersion: coreRtVersion,
                ilcPath: ilcPath,
                useCppCodeGenerator: useCppCodeGenerator,
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
                ilcGenerateStackTraceData: ilcGenerateStackTraceData
            );
        }
    }
}
