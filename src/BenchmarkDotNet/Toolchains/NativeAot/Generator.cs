using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Portability.Cpu;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.NativeAot
{
    /// <summary>
    /// generates new csproj file for self-contained NativeAOT app
    /// based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built
    /// and https://github.com/dotnet/corert/tree/7f902d4d8b1c3280e60f5e06c71951a60da173fb/samples/HelloWorld#add-corert-to-your-project
    /// </summary>
    public class Generator : CsProjGenerator
    {
        internal const string NativeAotNuGetFeed = "nativeAotNuGetFeed";
        internal const string GeneratedRdXmlFileName = "bdn_generated.rd.xml";

        internal Generator(string ilCompilerVersion,
            string runtimeFrameworkVersion, string targetFrameworkMoniker, string cliPath,
            string runtimeIdentifier, IReadOnlyDictionary<string, string> feeds, bool useNuGetClearTag,
            bool useTempFolderForRestore, string packagesRestorePath,
            bool rootAllApplicationAssemblies, bool ilcGenerateCompleteTypeMetadata, bool ilcGenerateStackTraceData,
            string ilcOptimizationPreference, string ilcInstructionSet)
            : base(targetFrameworkMoniker, cliPath, GetPackagesDirectoryPath(useTempFolderForRestore, packagesRestorePath), runtimeFrameworkVersion)
        {
            this.ilCompilerVersion = ilCompilerVersion;
            this.runtimeIdentifier = runtimeIdentifier;
            this.Feeds = feeds;
            this.useNuGetClearTag = useNuGetClearTag;
            this.useTempFolderForRestore = useTempFolderForRestore;
            this.rootAllApplicationAssemblies = rootAllApplicationAssemblies;
            this.ilcGenerateCompleteTypeMetadata = ilcGenerateCompleteTypeMetadata;
            this.ilcGenerateStackTraceData = ilcGenerateStackTraceData;
            this.ilcOptimizationPreference = ilcOptimizationPreference;
            this.ilcInstructionSet = ilcInstructionSet;
        }

        internal readonly IReadOnlyDictionary<string, string> Feeds;
        private readonly string ilCompilerVersion;
        private readonly string runtimeIdentifier;
        private readonly bool useNuGetClearTag;
        private readonly bool useTempFolderForRestore;
        private readonly bool rootAllApplicationAssemblies;
        private readonly bool ilcGenerateCompleteTypeMetadata;
        private readonly bool ilcGenerateStackTraceData;
        private readonly string ilcOptimizationPreference;
        private readonly string ilcInstructionSet;

        protected override string GetExecutableExtension() => RuntimeInformation.ExecutableExtension;

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
            => useTempFolderForRestore
                ? Path.Combine(Path.GetTempPath(), programName) // store everything in temp to avoid collisions with IDE
                : base.GetBuildArtifactsDirectoryPath(buildPartition, programName);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, runtimeIdentifier, "publish");

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string extraArguments = NativeAotToolchain.GetExtraArguments(runtimeIdentifier);

            var content = new StringBuilder(300)
                .AppendLine($"call {CliPath ?? "dotnet"} {DotNetCliCommand.GetRestoreCommand(artifactsPaths, buildPartition, extraArguments)}")
                .AppendLine($"call {CliPath ?? "dotnet"} {DotNetCliCommand.GetBuildCommand(artifactsPaths, buildPartition, extraArguments)}")
                .AppendLine($"call {CliPath ?? "dotnet"} {DotNetCliCommand.GetPublishCommand(artifactsPaths, buildPartition, extraArguments)}")
                .ToString();

            File.WriteAllText(artifactsPaths.BuildScriptFilePath, content);
        }

        // we always want to have a new directory for NuGet packages restore
        // to avoid this https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md#update-coreclr-using-runtime-nuget-package
        // some of the packages are going to contain source code, so they can not be in the subfolder of current solution
        // otherwise they would be compiled too (new .csproj include all .cs files from subfolders by default
        private static string GetPackagesDirectoryPath(bool useTempFolderForRestore, string packagesRestorePath)
            => packagesRestorePath ?? (useTempFolderForRestore ? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) : null);

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
            => useTempFolderForRestore
                ? base.GetArtifactsToCleanup(artifactsPaths).Concat(new[] { artifactsPaths.PackagesDirectoryName }).ToArray()
                : base.GetArtifactsToCleanup(artifactsPaths);

        protected override void GenerateNuGetConfig(ArtifactsPaths artifactsPaths)
        {
            if (!Feeds.Any())
                return;

            string content =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    {(useNuGetClearTag ? "<clear/>" : string.Empty)}
    {string.Join(Environment.NewLine + "    ", Feeds.Select(feed => $"<add key=\"{feed.Key}\" value=\"{feed.Value}\" />"))}
  </packageSources>
</configuration>";

            File.WriteAllText(artifactsPaths.NuGetConfigPath, content);
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            File.WriteAllText(artifactsPaths.ProjectFilePath, GenerateProjectForNuGetBuild(buildPartition, artifactsPaths, logger));
            GenerateReflectionFile(artifactsPaths);
        }

        private string GenerateProjectForNuGetBuild(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) => $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TargetFrameworkMoniker}</TargetFramework>
    <RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>
    <RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>
    <AssemblyName>{artifactsPaths.ProgramName}</AssemblyName>
    <AssemblyTitle>{artifactsPaths.ProgramName}</AssemblyTitle>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <PlatformTarget>{buildPartition.Platform.ToConfig()}</PlatformTarget>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugSymbols>false</DebugSymbols>
    <UseSharedCompilation>false</UseSharedCompilation>
    <Deterministic>true</Deterministic>
    <RunAnalyzers>false</RunAnalyzers>
    <PublishAot Condition="" '$(TargetFramework)' != 'net6.0' "">true</PublishAot>
    <IlcOptimizationPreference>{ilcOptimizationPreference}</IlcOptimizationPreference>
    {GetTrimmingSettings()}
    <IlcGenerateCompleteTypeMetadata>{ilcGenerateCompleteTypeMetadata}</IlcGenerateCompleteTypeMetadata>
    <IlcGenerateStackTraceData>{ilcGenerateStackTraceData}</IlcGenerateStackTraceData>
    <EnsureNETCoreAppRuntime>false</EnsureNETCoreAppRuntime> <!-- workaround for 'This runtime may not be supported by.NET Core.' error -->
    <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles> <!-- workaround for 'Found multiple publish output files with the same relative path.' error -->
    <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
    {GetInstructionSetSettings(buildPartition)}
  </PropertyGroup>
  {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    {GetILCompilerPackageReference()}
    <ProjectReference Include=""{GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).FullName}"" />
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, GetRdXmlFiles(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).Select(file => $"<RdXmlFile Include=\"{file}\" />"))}
  </ItemGroup>
</Project>";

        private string GetILCompilerPackageReference()
            => string.IsNullOrEmpty(ilCompilerVersion) ? "" : $@"<PackageReference Include=""Microsoft.DotNet.ILCompiler"" Version=""{ilCompilerVersion}"" />";

        private string GetTrimmingSettings()
            => rootAllApplicationAssemblies
                ? "" // use the defaults
                // TrimMode is set in explicit way as for older versions it might have different default value
                : "<TrimMode>link</TrimMode><TrimmerDefaultAction>link</TrimmerDefaultAction>";

        private string GetInstructionSetSettings(BuildPartition buildPartition)
        {
            string instructionSet = ilcInstructionSet ?? GetCurrentInstructionSet(buildPartition.Platform);
            return !string.IsNullOrEmpty(instructionSet) ? $"<IlcInstructionSet>{instructionSet}</IlcInstructionSet>" : "";
        }

        public IEnumerable<string> GetRdXmlFiles(Type benchmarkTarget, ILogger logger)
        {
            yield return GeneratedRdXmlFileName;

            var projectFile = GetProjectFilePath(benchmarkTarget, logger);
            var projectFileFolder = projectFile.DirectoryName;
            var rdXml = Path.Combine(projectFileFolder, "rd.xml");
            if (File.Exists(rdXml))
            {
                yield return rdXml;
            }

            foreach (var item in Directory.GetFiles(projectFileFolder, "*.rd.xml"))
            {
                yield return item;
            }
        }

        /// <summary>
        /// mandatory to make it possible to call GC.GetAllocatedBytesForCurrentThread() using reflection (not part of .NET Standard)
        /// </summary>
        private void GenerateReflectionFile(ArtifactsPaths artifactsPaths)
        {
            const string content = @"
<Directives>
    <Application>
        <Assembly Name=""System.Runtime"">
            <Type Name=""System.GC"" Dynamic=""Required All"" />
        </Assembly>
        <Assembly Name=""System.Threading.ThreadPool"">
            <Type Name=""System.Threading.ThreadPool"" Dynamic=""Required All"" />
        </Assembly>
        <Assembly Name=""System.Threading"">
            <Type Name=""System.Threading.Monitor"" Dynamic=""Required All"" />
        </Assembly>
    </Application>
</Directives>
";

            string directoryName = Path.GetDirectoryName(artifactsPaths.ProjectFilePath);
            if (directoryName != null)
                File.WriteAllText(Path.Combine(directoryName, GeneratedRdXmlFileName), content);
            else
                throw new InvalidOperationException($"Can't get directory of projectFilePath ('{artifactsPaths.ProjectFilePath}')");
        }

        private string GetCurrentInstructionSet(Platform platform)
            => string.Join(",", GetCurrentProcessInstructionSets(platform));

        // based on https://github.com/dotnet/runtime/blob/ce61c09a5f6fc71d8f717d3fc4562f42171869a0/src/coreclr/tools/Common/JitInterface/CorInfoInstructionSet.cs#L727
        private static IEnumerable<string> GetCurrentProcessInstructionSets(Platform platform)
        {
            switch (platform)
            {
                case Platform.X86:
                case Platform.X64:
                    if (HardwareIntrinsics.IsX86BaseSupported) yield return "base";
                    if (HardwareIntrinsics.IsX86SseSupported) yield return "sse";
                    if (HardwareIntrinsics.IsX86Sse2Supported) yield return "sse2";
                    if (HardwareIntrinsics.IsX86Sse3Supported) yield return "sse3";
                    if (HardwareIntrinsics.IsX86Ssse3Supported) yield return "ssse3";
                    if (HardwareIntrinsics.IsX86Sse41Supported) yield return "sse4.1";
                    if (HardwareIntrinsics.IsX86Sse42Supported) yield return "sse4.2";
                    if (HardwareIntrinsics.IsX86AvxSupported) yield return "avx";
                    if (HardwareIntrinsics.IsX86Avx2Supported) yield return "avx2";
                    if (HardwareIntrinsics.IsX86AesSupported) yield return "aes";
                    if (HardwareIntrinsics.IsX86Bmi1Supported) yield return "bmi";
                    if (HardwareIntrinsics.IsX86Bmi2Supported) yield return "bmi2";
                    if (HardwareIntrinsics.IsX86FmaSupported) yield return "fma";
                    if (HardwareIntrinsics.IsX86LzcntSupported) yield return "lzcnt";
                    if (HardwareIntrinsics.IsX86PclmulqdqSupported) yield return "pclmul";
                    if (HardwareIntrinsics.IsX86PopcntSupported) yield return "popcnt";
                    if (HardwareIntrinsics.IsX86AvxVnniSupported) yield return "avxvnni";
                    if (HardwareIntrinsics.IsX86SerializeSupported) yield return "serialize";
                    break;
                case Platform.Arm64:
                    if (HardwareIntrinsics.IsArmBaseSupported) yield return "base";
                    if (HardwareIntrinsics.IsArmAdvSimdSupported) yield return "neon";
                    if (HardwareIntrinsics.IsArmAesSupported) yield return "aes";
                    if (HardwareIntrinsics.IsArmCrc32Supported) yield return "crc";
                    if (HardwareIntrinsics.IsArmDpSupported) yield return "dotprod";
                    if (HardwareIntrinsics.IsArmRdmSupported) yield return "rdma";
                    if (HardwareIntrinsics.IsArmSha1Supported) yield return "sha1";
                    if (HardwareIntrinsics.IsArmSha256Supported) yield return "sha2";
                    // todo: handle "lse"
                    break;
                default:
                    yield break;
            }
        }
    }
}
