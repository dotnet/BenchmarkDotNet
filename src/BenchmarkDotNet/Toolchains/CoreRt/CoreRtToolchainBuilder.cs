using System;
using System.IO;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    public class CoreRtToolchainBuilder : CustomDotNetCliToolchainBuilder
    {
        public static CoreRtToolchainBuilder Create() => new CoreRtToolchainBuilder();

        private string coreRtVersion;
        private string ilcPath;
        private bool useCppCodeGenerator;
        
        private bool isCoreRtConfigured;

        /// <summary>
        /// creates a CoreRT toolchain targeting NuGet build of CoreRT
        /// Based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/samples/HelloWorld/README.md#add-corert-to-your-project
        /// </summary>
        /// <param name="microsoftDotNetILCompilerVersion">the version of Microsoft.DotNet.ILCompiler which should be used. The default is: "1.0.0-alpha-*"</param>
        /// <param name="nuGetFeedUrl">url to NuGet CoreRT feed, The default is: "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"</param>
        public CoreRtToolchainBuilder UseCoreRtNuGet(string microsoftDotNetILCompilerVersion = "1.0.0-alpha-*", string nuGetFeedUrl = "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json")
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
        public CoreRtToolchainBuilder UseCppCodeGenerator()
        {
            useCppCodeGenerator = true;

            return this;
        }

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
                targetFrameworkMoniker: targetFrameworkMoniker,
                runtimeIdentifier: runtimeIdentifier ?? GetPortableRuntimeIdentifier(),
                customDotNetCliPath: customDotNetCliPath,
                feeds: Feeds,
                useNuGetClearTag: useNuGetClearTag,
                useTempFolderForRestore: useTempFolderForRestore);
        }
    }
}