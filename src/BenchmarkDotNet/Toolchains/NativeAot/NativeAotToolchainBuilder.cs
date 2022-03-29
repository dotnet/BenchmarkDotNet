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
        private string ilcPath;
        private bool useCppCodeGenerator;
        private string packagesRestorePath;
        // we set those default values on purpose https://github.com/dotnet/BenchmarkDotNet/pull/1057#issuecomment-461832612
        private bool rootAllApplicationAssemblies;
        private bool ilcGenerateCompleteTypeMetadata = true;
        private bool ilcGenerateStackTraceData = true;

        private bool isIlCompilerConfigured;
        private string trimmerDefaultAction = "link";

        /// <summary>
        /// creates a NativeAOT toolchain targeting NuGet build of Microsoft.DotNet.ILCompiler
        /// Based on https://github.com/dotnet/runtimelab/blob/d0a37893a67c125f9b0cd8671846ff7d867df241/samples/HelloWorld/README.md#add-corert-to-your-project
        /// </summary>
        /// <param name="microsoftDotNetILCompilerVersion">the version of Microsoft.DotNet.ILCompiler which should be used. The default is: "7.0.0-*"</param>
        /// <param name="nuGetFeedUrl">url to NuGet feed, The default is: "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json"</param>
        [PublicAPI]
        public NativeAotToolchainBuilder UseNuGet(string microsoftDotNetILCompilerVersion = "7.0.0-*", string nuGetFeedUrl = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet7/nuget/v3/index.json")
        {
            ilCompilerVersion = microsoftDotNetILCompilerVersion ?? throw new ArgumentNullException(nameof(microsoftDotNetILCompilerVersion));

            Feeds[Generator.NativeAotNuGetFeed] = nuGetFeedUrl ?? throw new ArgumentNullException(nameof(nuGetFeedUrl));

            isIlCompilerConfigured = true;

            return this;
        }

        /// <summary>
        /// creates a Native toolchain targeting local build of ILCompiler
        /// Based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built
        /// </summary>
        /// <param name="newIlcPath">the ilcPath, an example: "C:\Projects\corert\bin\Windows_NT.x64.Release"</param>
        [PublicAPI]
        public NativeAotToolchainBuilder UseLocalBuild(string newIlcPath)
        {
            if (newIlcPath == null) throw new ArgumentNullException(nameof(newIlcPath));
            if (!Directory.Exists(newIlcPath)) throw new DirectoryNotFoundException($"{newIlcPath} provided as {nameof(newIlcPath)} does NOT exist");

            ilcPath = newIlcPath;
            useTempFolderForRestore = true;

            isIlCompilerConfigured = true;

            return this;
        }

        /// <summary>
        /// "This approach uses transpiler to convert IL to C++, and then uses platform specific C++ compiler and linker for compiling/linking the application.
        /// The transpiler is a lot less mature than the RyuJIT path. If you came here to give CoreRT a try" please don't use this option.
        /// Based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#using-cpp-code-generator
        /// </summary>
        [PublicAPI]
        public NativeAotToolchainBuilder UseCppCodeGenerator()
        {
            useCppCodeGenerator = true;

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
        /// By using the default value ("link") this ensures that the trimmer only analyzes the parts of the library's dependencies that are used.
        /// It tells the trimmer that any code that is not part of a "root" can be trimmed if it is unused.
        /// </summary>
        /// <remarks>Pass null or empty string to NOT set TrimmerDefaultAction to any value.</remarks>
        [PublicAPI]
        public NativeAotToolchainBuilder SeTrimmerDefaultAction(string value = "link")
        {
            trimmerDefaultAction = value;

            return this;
        }

        [PublicAPI]
        public override IToolchain ToToolchain()
        {
            if (!isIlCompilerConfigured)
                throw new InvalidOperationException("You need to use UseNuGet or UseLocalBuild methods to tell us which ILCompiler to use.");

            return new NativeAotToolchain(
                displayName: displayName ?? (ilCompilerVersion != null ? $"ILCompiler {ilCompilerVersion}" : "local ILCompiler build"),
                ilCompilerVersion: ilCompilerVersion,
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
                ilcGenerateStackTraceData: ilcGenerateStackTraceData,
                trimmerDefaultAction: trimmerDefaultAction
            );
        }
    }
}
