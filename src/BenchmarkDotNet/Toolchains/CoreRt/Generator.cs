using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.CoreRt
{
    /// <summary>
    /// generates new csproj file for self-contained .NET Core RT app
    /// based on https://github.com/dotnet/corert/blob/7f902d4d8b1c3280e60f5e06c71951a60da173fb/Documentation/how-to-build-and-run-ilcompiler-in-console-shell-prompt.md#compiling-source-to-native-code-using-the-ilcompiler-you-built 
    /// and https://github.com/dotnet/corert/tree/7f902d4d8b1c3280e60f5e06c71951a60da173fb/samples/HelloWorld#add-corert-to-your-project
    /// </summary>
    public class Generator : CsProjGenerator
    {
        internal const string CoreRtNuGetFeed = "coreRtNuGetFeed";

        internal Generator(string coreRtVersion, bool useCppCodeGenerator,
            string runtimeFrameworkVersion, string targetFrameworkMoniker,
            string runtimeIdentifier, IReadOnlyDictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore)
            : base(targetFrameworkMoniker, platform => platform.ToConfig(), runtimeFrameworkVersion)
        {
            this.coreRtVersion = coreRtVersion;
            this.useCppCodeGenerator = useCppCodeGenerator;
            this.targetFrameworkMoniker = targetFrameworkMoniker;
            this.runtimeIdentifier = runtimeIdentifier;
            this.feeds = feeds;
            this.useNuGetClearTag = useNuGetClearTag;
            this.useTempFolderForRestore = useTempFolderForRestore;
        }

        private readonly string coreRtVersion;
        private readonly bool useCppCodeGenerator;
        private readonly string targetFrameworkMoniker;
        private readonly string runtimeIdentifier;
        private readonly IReadOnlyDictionary<string, string> feeds;
        private readonly bool useNuGetClearTag;
        private readonly bool useTempFolderForRestore;

        private bool IsNuGetCoreRt => feeds.ContainsKey(CoreRtNuGetFeed) && !string.IsNullOrWhiteSpace(coreRtVersion);

        protected override string GetExecutableExtension() => RuntimeInformation.ExecutableExtension;

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
            => useTempFolderForRestore
                ? Path.Combine(Path.GetTempPath(), programName) // store everything in temp to avoid collisions with IDE
                : base.GetBuildArtifactsDirectoryPath(buildPartition, programName);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, runtimeIdentifier, "native");

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            string extraArguments = useCppCodeGenerator ? $"-r {runtimeIdentifier} /p:NativeCodeGen=cpp" : $"-r {runtimeIdentifier}";

            if (useTempFolderForRestore)
            {
                File.WriteAllText(artifactsPaths.BuildScriptFilePath,
                    $"dotnet restore --packages {artifactsPaths.PackagesDirectoryName} {extraArguments} --no-dependencies" + Environment.NewLine +
                    $"dotnet build -c {buildPartition.BuildConfiguration} {extraArguments} --no-restore --no-dependencies" + Environment.NewLine +
                    $"dotnet publish -c {buildPartition.BuildConfiguration} {extraArguments} --no-restore --no-dependencies");
            }
            else
            {
                File.WriteAllText(artifactsPaths.BuildScriptFilePath, $"dotnet publish -c {buildPartition.BuildConfiguration} {extraArguments}");
            }
        }

        // we always want to have a new directory for NuGet packages restore 
        // to avoid this https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md#update-coreclr-using-runtime-nuget-package
        // some of the packages are going to contain source code, so they can not be in the subfolder of current solution
        // otherwise they would be compiled too (new .csproj include all .cs files from subfolders by default
        protected override string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath)
            => useTempFolderForRestore
                ? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
                : null;

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
            => File.WriteAllText(artifactsPaths.ProjectFilePath, 
                    IsNuGetCoreRt 
                        ? GenerateProjectForNuGetBuild(buildPartition, artifactsPaths, logger) 
                        : GenerateProjectForLocalBuild(buildPartition, artifactsPaths, logger));

        private string GenerateProjectForNuGetBuild(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) => $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TargetFrameworkMoniker}</TargetFramework>
    <RuntimeIdentifier>{runtimeIdentifier}</RuntimeIdentifier>
    <RuntimeFrameworkVersion>{RuntimeFrameworkVersion}</RuntimeFrameworkVersion>
    <AssemblyName>{artifactsPaths.ProgramName}</AssemblyName>
    <AssemblyTitle>{artifactsPaths.ProgramName}</AssemblyTitle>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugType>pdbonly</DebugType>
    <DebugSymbols>true</DebugSymbols>
  </PropertyGroup>
  {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.DotNet.ILCompiler"" Version=""{coreRtVersion}"" />
    <ProjectReference Include=""{GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).FullName}"" />
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
    <DebugType>pdbonly</DebugType>
    <DebugSymbols>true</DebugSymbols>
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
</Project>";

    }
}