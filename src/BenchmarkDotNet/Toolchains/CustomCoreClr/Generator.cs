using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    /// <summary>
    /// generates new csproj file for self-contained .NET Core app which uses given CoreCLR NuGet packages
    /// based on https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md and https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md
    /// </summary>
    public class Generator : CsProjGenerator
    {
        internal const string LocalCoreClrPackagesBin = "localCoreClrPackagesBin";
        internal const string LocalCoreClrPackages = "localCoreClrPackages";
        internal const string CoreClrNuGetFeed = "coreClrNuGetFeed";
        internal const string LocalCoreFxPackagesBin = "localCoreFxPacakgesBin";
        internal const string CoreFxNuGetFeed = "coreFxNuGetFeed";

        internal Generator(string coreClrVersion, string coreFxVersion, string runtimeFrameworkVersion, string targetFrameworkMoniker,
            string runtimeIdentifier, IReadOnlyDictionary<string, string> feeds, bool useNuGetClearTag, bool useTempFolderForRestore)
            : base(targetFrameworkMoniker, platform => platform.ToConfig(), runtimeFrameworkVersion)
        {
            this.coreClrVersion = coreClrVersion;
            this.coreFxVersion = coreFxVersion;
            this.targetFrameworkMoniker = targetFrameworkMoniker;
            this.runtimeIdentifier = runtimeIdentifier;
            this.feeds = feeds;
            this.useNuGetClearTag = useNuGetClearTag;
            this.useTempFolderForRestore = useTempFolderForRestore;
        }

        private readonly string coreClrVersion;
        private readonly string coreFxVersion;
        private readonly string targetFrameworkMoniker;
        private readonly string runtimeIdentifier;
        private readonly IReadOnlyDictionary<string, string> feeds;
        private readonly bool useNuGetClearTag;
        private readonly bool useTempFolderForRestore;

        private bool IsUsingCustomCoreClr => feeds.ContainsKey(LocalCoreClrPackagesBin) || feeds.ContainsKey(CoreClrNuGetFeed);
        private bool IsUsingCustomCoreFx => feeds.ContainsKey(LocalCoreFxPackagesBin) || feeds.ContainsKey(CoreFxNuGetFeed);

        protected override string GetExecutableExtension() => RuntimeInformation.ExecutableExtension;

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
            => useTempFolderForRestore
                ? Path.Combine(Path.GetTempPath(), programName) // store everything in temp to avoid collisions with IDE
                : base.GetBuildArtifactsDirectoryPath(buildPartition, programName);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, runtimeIdentifier);   

        protected override void GenerateBuildScript(BuildPartition buildPartition, ArtifactsPaths artifactsPaths)
        {
            if (useTempFolderForRestore)
            {
                File.WriteAllText(artifactsPaths.BuildScriptFilePath,
                    $"dotnet restore --packages {artifactsPaths.PackagesDirectoryName} --no-dependencies" + Environment.NewLine +
                    $"dotnet build -c {buildPartition.BuildConfiguration} --no-restore --no-dependencies" + Environment.NewLine +
                    $"dotnet publish -c {buildPartition.BuildConfiguration} --no-restore --no-dependencies");
            }
            else
            {
                File.WriteAllText(artifactsPaths.BuildScriptFilePath, $"dotnet publish -c {buildPartition.BuildConfiguration}");
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
            string csProj = $@"
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
    <PackageConflictPreferredPackages>runtime.{runtimeIdentifier}.Microsoft.NETCore.Runtime.CoreCLR;runtime.{runtimeIdentifier}.Microsoft.NETCore.Jit;runtime.{runtimeIdentifier}.Microsoft.Private.CoreFx.NETCoreApp;Microsoft.Private.CoreFx.NETCoreApp;Microsoft.NETCore.App;$(PackageConflictPreferredPackages)</PackageConflictPreferredPackages>
  </PropertyGroup>
  {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, GetReferences(buildPartition.RepresentativeBenchmarkCase, logger))}
  </ItemGroup>
</Project>";

            File.WriteAllText(artifactsPaths.ProjectFilePath, csProj);
        }

        private IEnumerable<string> GetReferences(BenchmarkCase benchmarkCase, ILogger logger)
        {
            if (IsUsingCustomCoreClr)
            {
                yield return $@"<PackageReference Include=""runtime.{runtimeIdentifier}.Microsoft.NETCore.Runtime.CoreCLR"" Version=""{coreClrVersion}"" />";
                yield return $@"<PackageReference Include=""runtime.{runtimeIdentifier}.Microsoft.NETCore.Jit"" Version=""{coreClrVersion}"" />";
            }

            if (IsUsingCustomCoreFx)
            {
                yield return $@"<PackageReference Include=""runtime.{runtimeIdentifier}.Microsoft.Private.CoreFx.NETCoreApp"" Version=""{coreFxVersion}"" />";
            }

            yield return $@"<ProjectReference Include=""{GetProjectFilePath(benchmarkCase.Descriptor.Type, logger).FullName}"" />";
        }
    }
}