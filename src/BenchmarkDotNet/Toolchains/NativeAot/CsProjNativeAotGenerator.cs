using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Cpu;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using System.Text;
using System.Xml;

namespace BenchmarkDotNet.Toolchains.NativeAot;

/// <summary>
/// Generates new csproj file for self-contained NativeAOT app.
/// </summary>
internal sealed class CsProjNativeAotGenerator : CsProjGenerator
{
    internal const string NativeAotNuGetFeed = "nativeAotNuGetFeed";
    private const string DefaultNuGetFeed = "https://api.nuget.org/v3/index.json";
    private const string LocalBuildDotNetFeed = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet11/nuget/v3/index.json";
    internal const string GeneratedRdXmlFileName = "bdn_generated.rd.xml";

    private readonly NativeAotSettings settings;

    internal CsProjNativeAotGenerator(NativeAotSettings settings)
        : base(settings with { PackagesPath = GetPackagesDirectoryPath(settings.UseTempFolderForRestore, settings.PackagesPath) })
    {
        this.settings = settings;
        BenchmarkRunCallType = Code.CodeGenBenchmarkRunCallType.Direct;
    }

    protected override string GetExecutableExtension() => OsDetector.ExecutableExtension;

    protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
        => settings.UseTempFolderForRestore
            ? Path.Combine(Path.GetTempPath(), programName) // store everything in temp to avoid collisions with IDE
            : base.GetBuildArtifactsDirectoryPath(buildPartition, programName);

    protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, settings.RuntimeIdentifier, "publish");

    protected override ValueTask GenerateBuildScriptAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
    {
        string projectFilePath = GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, NullLogger.Instance).FullName;
        string extraArguments = CsProjNativeAotToolchain.GetExtraArguments(settings.RuntimeIdentifier);

        string cli = Settings.CliPath?.FullName ?? DotNetCliCommandExecutor.DefaultDotNetCliPath.Value;
        var content = new StringBuilder(300)
            .AppendLine($"call {cli} {DotNetCliCommand.GetRestoreCommand(artifactsPaths, buildPartition, projectFilePath, extraArguments)}")
            .AppendLine($"call {cli} {DotNetCliCommand.GetPublishCommand(artifactsPaths, buildPartition, projectFilePath, Settings.TargetFrameworkMoniker, extraArguments)}")
            .AppendLine($"call {cli} {DotNetCliCommand.GetRestoreCommand(artifactsPaths, buildPartition, artifactsPaths.ProjectFilePath, extraArguments)}")
            .AppendLine($"call {cli} {DotNetCliCommand.GetPublishCommand(artifactsPaths, buildPartition, artifactsPaths.ProjectFilePath, Settings.TargetFrameworkMoniker, extraArguments)}")
            .ToString();

        return new(File.WriteAllTextAsync(artifactsPaths.BuildScriptFilePath, content, cancellationToken));
    }

    // We always want to have a new directory for NuGet packages restore.
    // Some of the packages are going to contain source code, so they can not be in the subfolder of current solution
    // otherwise they would be compiled too (new .csproj include all .cs files from subfolders by default).
    private static DirectoryInfo? GetPackagesDirectoryPath(bool useTempFolderForRestore, DirectoryInfo? packagesRestorePath)
        => packagesRestorePath is null && useTempFolderForRestore
               ? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
               : null;

    protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths)
        => settings.UseTempFolderForRestore && artifactsPaths.PackagesDirectoryName.IsNotBlank()
            ? base.GetArtifactsToCleanup(artifactsPaths).Concat([artifactsPaths.PackagesDirectoryName]).ToArray()
            : base.GetArtifactsToCleanup(artifactsPaths);

    protected override async ValueTask GenerateNuGetConfigAsync(ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
    {
        var feeds = GetFeeds();
        if (feeds.Length == 0)
            return;

        // Skip creating a NuGet.config if the clear tag is not specified and the only feed is the default nuget.org feed.
        if (!settings.UseNuGetClearTag && feeds is [{ Value: DefaultNuGetFeed }])
            return;

        string content = $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            {(settings.UseNuGetClearTag ? "<clear/>" : string.Empty)}
            {string.Join(Environment.NewLine + "    ", feeds.Select(feed => $"<add key=\"{feed.Key}\" value=\"{feed.Value}\" />"))}
          </packageSources>
        </configuration>
        """;

        await File.WriteAllTextAsync(artifactsPaths.NuGetConfigPath, content, cancellationToken).ConfigureAwait(false);
    }

    // The ILCompiler is restored either from a NuGet feed, or from a local runtime build (which also needs the dotnet nightly feed).
    private KeyValuePair<string, string>[] GetFeeds()
        => settings.LocalIlcPackages is not null ? [new("local", settings.LocalIlcPackages.FullName), new("dotnet11", LocalBuildDotNetFeed)]
        : settings.NuGetFeedUrl.IsNotBlank() ? [new(NativeAotNuGetFeed, settings.NuGetFeedUrl!)]
        : [];

    protected override async ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
    {
        var projectFile = GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).FullName;

        await File.WriteAllTextAsync(artifactsPaths.ProjectFilePath, GenerateProjectForNuGetBuild(projectFile, buildPartition, artifactsPaths, logger), cancellationToken).ConfigureAwait(false);

        // Generate `bdn_generated.rd.xml`
        await GenerateReflectionFileAsync(artifactsPaths, cancellationToken).ConfigureAwait(false);

        // Integration tests are built without dependencies, so we skip gathering dlls.
        if (buildPartition.ForcedNoDependenciesForIntegrationTests)
            return;

        await GatherReferencesAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
    }

    private string GenerateProjectForNuGetBuild(string projectFilePath, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger) => $"""
    <Project Sdk="Microsoft.NET.Sdk">
      <Import Project="$(MSBuildThisFileDirectory)BenchmarkDotNet.Build.props" />
      <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFrameworks>{Settings.TargetFrameworkMoniker}</TargetFrameworks>
        <RuntimeIdentifier>{settings.RuntimeIdentifier}</RuntimeIdentifier>
        <AssemblyName>{artifactsPaths.ProgramName}</AssemblyName>
        <AssemblyTitle>{artifactsPaths.ProgramName}</AssemblyTitle>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
        <PlatformTarget>{buildPartition.Platform.ToConfig()}</PlatformTarget>
        <TreatWarningsAsErrors>False</TreatWarningsAsErrors>
        <MSBuildTreatWarningsAsErrors>false</MSBuildTreatWarningsAsErrors>
        <DebugSymbols>false</DebugSymbols>
        <RunAnalyzers>false</RunAnalyzers>
        <PublishAot>true</PublishAot>
        <IlcOptimizationPreference>{settings.OptimizationPreference}</IlcOptimizationPreference>
        <OptimizationPreference>{settings.OptimizationPreference}</OptimizationPreference>
        <IlcGenerateStackTraceData>{settings.GenerateStackTraceData}</IlcGenerateStackTraceData>
        <StackTraceSupport>{settings.GenerateStackTraceData}</StackTraceSupport>
        <EnsureNETCoreAppRuntime>false</EnsureNETCoreAppRuntime> <!-- workaround for 'This runtime may not be supported by.NET Core.' error -->
        <ValidateExecutableReferencesMatchSelfContained>false</ValidateExecutableReferencesMatchSelfContained>
        {GetInstructionSetSettings(buildPartition)}
      </PropertyGroup>
      {GetRuntimeSettings(buildPartition.RepresentativeBenchmarkCase.Job.Environment.Gc, buildPartition.Resolver)}
      <ItemGroup>
        <Compile Include="{Path.GetFileName(artifactsPaths.ProgramCodePath)}" Exclude="bin\**;obj\**;**\*.xproj;packages\**" />
      </ItemGroup>
      <ItemGroup>
        {GetILCompilerPackageReference()}
        <ProjectReference Include="{projectFilePath}" />
      </ItemGroup>
      <ItemGroup>
        {string.Join(Environment.NewLine, GetRdXmlFiles(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger).Select(file => $"<RdXmlFile Include=\"{file}\" />"))}
      </ItemGroup>
      {GetCustomProperties(buildPartition, logger)}
      <!-- Set LangVersion after copied settings so it overrides any LangVersion copied from the benchmarks project -->
      <PropertyGroup>
        <LangVersion Condition="'$(LangVersion)' == '' Or ($([System.Char]::IsDigit('$(LangVersion)', 0)) And '$(LangVersion)' &lt; '8.0')">latest</LangVersion>
      </PropertyGroup>
      <Import Project="$(MSBuildThisFileDirectory)BenchmarkDotNet.Build.targets" />
    </Project>
    """;

    private string GetCustomProperties(BuildPartition buildPartition, ILogger logger)
    {
        var projectFile = GetProjectFilePath(buildPartition.RepresentativeBenchmarkCase.Descriptor.Type, logger);
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(projectFile.FullName);

        (string customProperties, _) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);
        return customProperties;
    }


    private string GetILCompilerPackageReference()
        => settings.IlCompilerVersion.IsBlank() ? "" : $@"<PackageReference Include=""Microsoft.DotNet.ILCompiler"" Version=""{settings.IlCompilerVersion}"" />";

    private string GetInstructionSetSettings(BuildPartition buildPartition)
    {
        string instructionSet = settings.InstructionSet.IsBlank()
            ? GetCurrentInstructionSet(buildPartition.Platform)
            : settings.InstructionSet;

        return instructionSet.IsNotBlank()
            ? $"<IlcInstructionSet>{instructionSet}</IlcInstructionSet>"
            : "";
    }

    public IEnumerable<string> GetRdXmlFiles(Type benchmarkTarget, ILogger logger)
    {
        yield return GeneratedRdXmlFileName;

        var projectFile = GetProjectFilePath(benchmarkTarget, logger);
        var projectFileFolder = projectFile.DirectoryName!;
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
    private ValueTask GenerateReflectionFileAsync(ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
    {
        const string content = """
        <Directives>
            <Application>
                <Assembly Name="System.Runtime">
                    <Type Name="System.GC" Dynamic="Required All" />
                </Assembly>
                <Assembly Name="System.Threading.ThreadPool">
                    <Type Name="System.Threading.ThreadPool" Dynamic="Required All" />
                </Assembly>
                <Assembly Name="System.Threading">
                    <Type Name="System.Threading.Monitor" Dynamic="Required All" />
                </Assembly>
            </Application>
        </Directives>

        """;

        string directoryName = Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!;
        if (directoryName == null)
            throw new InvalidOperationException($"Can't get directory of projectFilePath ('{artifactsPaths.ProjectFilePath}')");

        return new(File.WriteAllTextAsync(Path.Combine(directoryName, GeneratedRdXmlFileName), content, cancellationToken));
    }

    private string GetCurrentInstructionSet(Platform platform)
        => string.Join(",", GetCurrentProcessInstructionSets(platform));

    // based on https://github.com/dotnet/runtime/tree/v10.0.0-rc.1.25451.107/src/coreclr/tools/Common/JitInterface/ThunkGenerator/InstructionSetDesc.txt
    private IEnumerable<string> GetCurrentProcessInstructionSets(Platform platform)
    {
        if (!Runtime.TryParse(Settings.TargetFrameworkMoniker, out Runtime? runtime))
        {
            throw new NotSupportedException($"Invalid TFM: '{Settings.TargetFrameworkMoniker}'");
        }

        // The instruction sets recognized by ILC depend on the .NET version being compiled; gate on the version directly.
        Version version = runtime.Version!;

        if (platform == RuntimeInformation.GetCurrentPlatform() // "native" does not support cross-compilation (so does BDN for now)
            && version.Major >= 8)
        {
            yield return "native"; // added in .NET 8 https://github.com/dotnet/runtime/pull/87865
            yield break;
        }

        switch (platform)
        {
            case Platform.X86:
            case Platform.X64:
                if (HardwareIntrinsics.IsX86BaseSupported) yield return "base";
                if (HardwareIntrinsics.IsX86Sse42Supported)
                {
                    if (version.Major <= 10) yield return "sse4.2";
                    if (version.Major <= 9) yield return "popcnt";
                }
                if (HardwareIntrinsics.IsX86AvxSupported) yield return "avx";
                if (HardwareIntrinsics.IsX86Avx2Supported)
                {
                    yield return "avx2";

                    if (version.Major <= 9)
                    {
                        yield return "bmi";
                        yield return "bmi2";
                        yield return "fma";
                        yield return "lzcnt";
                    }
                }
                if (HardwareIntrinsics.IsX86Avx512Supported && (version.Major > 8))
                {
                    if (version.Major >= 10)
                    {
                        yield return "avx512";
                    }
                    else
                    {
                        yield return "avx512f";
                        yield return "avx512f_vl";
                        yield return "avx512bw";
                        yield return "avx512bw_vl";
                        yield return "avx512cd";
                        yield return "avx512cd_vl";
                        yield return "avx512dq";
                        yield return "avx512dq_vl";
                    }
                }
                if (HardwareIntrinsics.IsX86Avx512v2Supported && (version.Major > 8))
                {
                    if (version.Major >= 10)
                    {
                        yield return "avx512v2";
                    }
                    else
                    {
                        yield return "avx512vbmi";
                        yield return "avx512vbmi_vl";
                    }
                }
                if (HardwareIntrinsics.IsX86Avx512v3Supported && (version.Major >= 10)) yield return "avx512v3";
                if (HardwareIntrinsics.IsX86Avx10v1Supported && (version.Major >= 9)) yield return "avx10v1";
                if (HardwareIntrinsics.IsX86Avx10v2Supported && (version.Major >= 10)) yield return "avx10v2";
                if (HardwareIntrinsics.IsX86AesSupported)
                {
                    yield return "aes";
                    if (version.Major <= 9) yield return "pclmul";
                }
                if (HardwareIntrinsics.IsX86AvxVnniSupported) yield return "avxvnni";
                if (HardwareIntrinsics.IsX86SerializeSupported && version.Major > 7) yield return "serialize"; // https://github.com/dotnet/BenchmarkDotNet/issues/2463#issuecomment-1809625008
                break;
            case Platform.Arm64:
                if (HardwareIntrinsics.IsArmBaseSupported)
                {
                    yield return "base";
                    yield return "neon";
                }
                if (HardwareIntrinsics.IsArmAesSupported) yield return "aes";
                if (HardwareIntrinsics.IsArmCrc32Supported) yield return "crc";
                if (HardwareIntrinsics.IsArmDpSupported) yield return "dotprod";
                if (HardwareIntrinsics.IsArmRdmSupported) yield return "rdma";
                if (HardwareIntrinsics.IsArmSha1Supported) yield return "sha1";
                if (HardwareIntrinsics.IsArmSha256Supported) yield return "sha2";
                break;
            default:
                yield break;
        }
    }
}
