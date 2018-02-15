using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using Microsoft.DotNet.PlatformAbstractions;
using System;
using System.Collections.Generic;
using System.IO;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    /// <summary>
    /// generates new csproj file for self-contained .NET Core app which uses local/preview CoreCLR build
    /// based on https://github.com/dotnet/coreclr/blob/master/Documentation/workflow/UsingDotNetCli.md and https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/dogfooding.md
    /// </summary>
    public class Generator : CsProjGenerator
    {
        /// <param name="coreClrNuGetFeed">path to folder for local CoreCLR builds, url to MyGet feed for previews of CoreCLR. Example: "C:\coreclr\bin\Product\Windows_NT.x64.Debug\.nuget\pkg"</param>
        /// <param name="coreClrVersion">the version of Microsoft.NETCore.Runtime which should be used. Example: "2.1.0-preview2-26305-0"</param>
        /// <param name="coreFxNuGetFeed">path to folder for local CoreFX builds, url to MyGet feed for previews of CoreFX. Example: "C:\Projects\forks\corefx\bin\packages\Debug"</param>
        /// <param name="coreFxVersion">the version of Microsoft.Private.CoreFx.NETCoreApp which should be used. Example: 4.5.0-preview2-26307-0</param>
        /// <param name="targetFrameworkMoniker">TFM, netcoreapp2.1 is the default</param>
        /// <param name="runtimeIdentifier">if not provided, portable OS-arch will be used (example: "win-x64", "linux-x86")</param>
        public Generator(string coreClrNuGetFeed, string coreClrVersion,
            string coreFxNuGetFeed, string coreFxVersion,
            string targetFrameworkMoniker = "netcoreapp2.1",
            string runtimeIdentifier = null)
            : base(targetFrameworkMoniker, platform => platform.ToString())
        {
            CoreClrNuGetFeed = coreClrNuGetFeed;
            CoreClrVersion = coreClrVersion;
            CoreFxNuGetFeed = coreFxNuGetFeed;
            CoreFxVersion = coreFxVersion;
            RuntimeIdentifier = runtimeIdentifier ?? GetPortableRuntimeIdentifier();
        }

        private string CoreClrNuGetFeed { get; }
        private string CoreClrVersion { get; }
        private string CoreFxNuGetFeed { get; }
        private string CoreFxVersion { get; }
        private string RuntimeIdentifier { get; }

        private bool IsLocalCoreClr => Directory.Exists(CoreClrNuGetFeed);
        private bool IsLocalCoreFx => Directory.Exists(CoreFxNuGetFeed);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, RuntimeIdentifier, "publish");

        protected override void GenerateBuildScript(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver)
            => File.WriteAllText(artifactsPaths.BuildScriptFilePath, $"dotnet publish -c Release"); // publish does the restore

        protected override void GenerateNuGetConfig(Benchmark benchmark, ArtifactsPaths artifactsPaths)
        {
            string content =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <!--To inherit the global NuGet package sources remove the <clear/> line below -->
    <clear />

    <add key=""{nameof(CoreClrNuGetFeed)}"" value=""{CoreClrNuGetFeed}"" />
    <add key=""{nameof(CoreFxNuGetFeed)}"" value=""{CoreFxNuGetFeed}"" />
  </packageSources>
</configuration>";

            File.WriteAllText(artifactsPaths.NuGetConfigPath, content);
        }

        protected override void GenerateProject(Benchmark benchmark, ArtifactsPaths artifactsPaths, IResolver resolver, ILogger logger)
        {
            string csProj = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{TargetFrameworkMoniker}</TargetFramework>
    <RuntimeIdentifier>{RuntimeIdentifier}</RuntimeIdentifier>
    <AssemblyName>{artifactsPaths.ProgramName}</AssemblyName>
    <AssemblyTitle>{artifactsPaths.ProgramName}</AssemblyTitle>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
    <DebugType>pdbonly</DebugType>
    <DebugSymbols>true</DebugSymbols>
    <PackageConflictPreferredPackages>Microsoft.Private.CoreFx.NETCoreApp;runtime.{RuntimeIdentifier}.Microsoft.Private.CoreFx.NETCoreApp;Microsoft.NETCore.App;$(PackageConflictPreferredPackages)</PackageConflictPreferredPackages>
  </PropertyGroup>
  {GetRuntimeSettings(benchmark.Job.Env.Gc, resolver)}
  <ItemGroup>
    <Compile Include=""{Path.GetFileName(artifactsPaths.ProgramCodePath)}"" Exclude=""bin\**;obj\**;**\*.xproj;packages\**"" />
  </ItemGroup>
  <ItemGroup>
    {string.Join(Environment.NewLine, GetReferences(benchmark, logger))}
  </ItemGroup>
</Project>";

            File.WriteAllText(artifactsPaths.ProjectFilePath, csProj);
        }

        private IEnumerable<string> GetReferences(Benchmark benchmark, ILogger logger)
        {
            var coreClrPackagesPrefix = IsLocalCoreClr ? $"runtime.{RuntimeIdentifier}." : null;
            var coreFxPackagesPrefix = IsLocalCoreFx ? $"runtime.{RuntimeIdentifier}." : null;

            yield return $@"<PackageReference Include=""{coreClrPackagesPrefix}Microsoft.NETCore.Runtime.CoreCLR"" Version=""{CoreClrVersion}"" />";
            yield return $@"<PackageReference Include=""{coreClrPackagesPrefix}Microsoft.NETCore.Jit"" Version=""{CoreClrVersion}"" />";
            
            yield return $@"<PackageReference Include=""{coreFxPackagesPrefix}Microsoft.Private.CoreFx.NETCoreApp"" Version=""{CoreFxVersion}"" />";

            yield return $@"<ProjectReference Include=""{GetProjectFilePath(benchmark.Target.Type, logger).FullName}"" />";
        }

        private static string GetPortableRuntimeIdentifier()
        {
            // Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment.GetRuntimeIdentifier()
            // returns win10-x64, we want the simpler form win-x64
            // the values taken from https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#macos-rids
            string osPart = RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows
                ? "win" : (RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux ? "linux" : "osx");

            return $"{osPart}-{RuntimeEnvironment.RuntimeArchitecture}";
        }
    }
}
