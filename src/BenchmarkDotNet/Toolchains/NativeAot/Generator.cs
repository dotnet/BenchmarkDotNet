using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
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
            this.targetFrameworkMoniker = targetFrameworkMoniker;
            this.runtimeIdentifier = runtimeIdentifier;
            this.feeds = feeds;
            this.useNuGetClearTag = useNuGetClearTag;
            this.useTempFolderForRestore = useTempFolderForRestore;
            this.rootAllApplicationAssemblies = rootAllApplicationAssemblies;
            this.ilcGenerateCompleteTypeMetadata = ilcGenerateCompleteTypeMetadata;
            this.ilcGenerateStackTraceData = ilcGenerateStackTraceData;
            this.ilcOptimizationPreference = ilcOptimizationPreference;
            this.ilcInstructionSet = ilcInstructionSet;
        }

        private readonly string ilCompilerVersion;
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        private readonly string targetFrameworkMoniker;
        private readonly string runtimeIdentifier;
        private readonly IReadOnlyDictionary<string, string> feeds;
        private readonly bool useNuGetClearTag;
        private readonly bool useTempFolderForRestore;
        private readonly bool rootAllApplicationAssemblies;
        private readonly bool ilcGenerateCompleteTypeMetadata;
        private readonly bool ilcGenerateStackTraceData;
        private readonly string ilcOptimizationPreference;
        private readonly string ilcInstructionSet;

        private bool IsNuGet => feeds.ContainsKey(NativeAotNuGetFeed) && !string.IsNullOrWhiteSpace(ilCompilerVersion);

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
            if (!feeds.Any())
                return;

            string content =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    {(useNuGetClearTag ? "<clear/>" : string.Empty)}
    {string.Join(Environment.NewLine + "    ", feeds.Select(feed => $"<add key=\"{feed.Key}\" value=\"{feed.Value}\" />"))}
  </packageSources>
</configuration>";

            File.WriteAllText(artifactsPaths.NuGetConfigPath, content);
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            File.WriteAllText(artifactsPaths.ProjectFilePath,
                IsNuGet
                    ? GenerateProjectForNuGetBuild(buildPartition, artifactsPaths, logger)
                    : GenerateProjectForLocalBuild(buildPartition, artifactsPaths, logger));

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
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugSymbols>false</DebugSymbols>
    <UseSharedCompilation>false</UseSharedCompilation>
    <Deterministic>true</Deterministic>
    <RunAnalyzers>false</RunAnalyzers>
    <IlcOptimizationPreference>{ilcOptimizationPreference}</IlcOptimizationPreference>
    {GetTrimmingSettings()}
    <IlcGenerateCompleteTypeMetadata>{ilcGenerateCompleteTypeMetadata}</IlcGenerateCompleteTypeMetadata>
    <IlcGenerateStackTraceData>{ilcGenerateStackTraceData}</IlcGenerateStackTraceData>
    <EnsureNETCoreAppRuntime>false</EnsureNETCoreAppRuntime> <!-- workaround for 'This runtime may not be supported by.NET Core.' error -->
    <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
  </PropertyGroup>
  {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.DotNet.ILCompiler"" Version=""{ilCompilerVersion}"" />
    <ProjectReference Include=""{GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).FullName}"" />
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, GetRdXmlFiles(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).Select(file => $"<RdXmlFile Include=\"{file}\" />"))}
  </ItemGroup>
  <ItemGroup>
    {GetInstructionSetSettings()}
  </ItemGroup>
</Project>";

        private string GenerateProjectForLocalBuild(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) => $@"
<Project>
  <Import Project=""$(MSBuildSDKsPath)\Microsoft.NET.Sdk\Sdk\Sdk.props"" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TargetFrameworkMoniker}</TargetFramework>
    <RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>
    <RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>
    <AssemblyName>{artifactsPaths.ProgramName}</AssemblyName>
    <AssemblyTitle>{artifactsPaths.ProgramName}</AssemblyTitle>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugSymbols>false</DebugSymbols>
    <UseSharedCompilation>false</UseSharedCompilation>
    <Deterministic>true</Deterministic>
    <RunAnalyzers>false</RunAnalyzers>
    <IlcOptimizationPreference>{ilcOptimizationPreference}</IlcOptimizationPreference>
    {GetTrimmingSettings()}
    <IlcGenerateCompleteTypeMetadata>{ilcGenerateCompleteTypeMetadata}</IlcGenerateCompleteTypeMetadata>
    <IlcGenerateStackTraceData>{ilcGenerateStackTraceData}</IlcGenerateStackTraceData>
    <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
  </PropertyGroup>
  <Import Project=""$(MSBuildSDKsPath)\Microsoft.NET.Sdk\Sdk\Sdk.targets"" />
  <Import Project=""$(IlcPath)\build\Microsoft.NETCore.Native.targets"" />
  {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""{GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).FullName}"" />
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, GetRdXmlFiles(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).Select(file => $"<RdXmlFile Include=\"{file}\" />"))}
  </ItemGroup>
  <ItemGroup>
    {GetInstructionSetSettings()}
  </ItemGroup>
</Project>";

        private string GetTrimmingSettings()
            => rootAllApplicationAssemblies
                ? "" // use the defaults
                // TrimMode is set in explicit way as for older versions it might have different default value
                : "<TrimMode>link</TrimMode><TrimmerDefaultAction>link</TrimmerDefaultAction>";

        private string GetInstructionSetSettings()
            => !string.IsNullOrEmpty(ilcInstructionSet)
                ? $@"<IlcArg Include=""--instructionset:{ilcInstructionSet}"" />"
                : "";

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
    }
}
