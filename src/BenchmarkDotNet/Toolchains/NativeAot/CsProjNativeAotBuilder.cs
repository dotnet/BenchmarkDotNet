using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Detectors.Cpu;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Xml.Linq;

namespace BenchmarkDotNet.Toolchains.NativeAot;

/// <summary>
/// Generates new csproj file for self-contained NativeAOT app.
/// </summary>
internal sealed class CsProjNativeAotBuilder : CsProjBuilder
{
    internal const string NativeAotNuGetFeed = "nativeAotNuGetFeed";
    private const string DefaultNuGetFeed = "https://api.nuget.org/v3/index.json";
    private const string LocalBuildDotNetFeed = "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet12/nuget/v3/index.json";
    internal const string GeneratedRdXmlFileName = "bdn_generated.rd.xml";

    private readonly NativeAotSettings settings;

    internal CsProjNativeAotBuilder(NativeAotSettings settings)
        : base(settings)
    {
        this.settings = settings;
        BenchmarkRunCallType = Code.CodeGenBenchmarkRunCallType.Direct;
    }

    protected override string GetExecutableExtension() => OsDetector.ExecutableExtension;

    protected override bool PublishesOutput => true;

    protected override async ValueTask GenerateNuGetConfigAsync(ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
    {
        var feeds = GetFeeds();
        if (feeds.Length == 0)
            return;

        // Skip creating a NuGet.config if the clear tag is not specified and the only feed is the default nuget.org feed.
        if (!settings.UseNuGetClearTag && feeds is [{ Value: DefaultNuGetFeed }])
            return;

        var configuration = new XElement("configuration",
            new XElement("packageSources",
                settings.UseNuGetClearTag ? new XElement("clear") : null,
                feeds.Select(feed => new XElement("add",
                    new XAttribute("key", feed.Key),
                    new XAttribute("value", feed.Value)))));

        await SaveXmlAsync(new XDocument(configuration), artifactsPaths.NuGetConfigPath, cancellationToken).ConfigureAwait(false);
    }

    // The ILCompiler is restored either from a NuGet feed, or from a local runtime build (which also needs the dotnet nightly feed).
    private KeyValuePair<string, string>[] GetFeeds()
        => settings.LocalIlcPackages is not null ? [new("local", settings.LocalIlcPackages.FullName), new("dotnet12", LocalBuildDotNetFeed)]
        : settings.NuGetFeedUrl.IsNotBlank() ? [new(NativeAotNuGetFeed, settings.NuGetFeedUrl!)]
        : [];

    protected override async ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
    {
        // The project points at this file, so it has to exist before it is built to gather references.
        await GenerateReflectionFileAsync(artifactsPaths, cancellationToken).ConfigureAwait(false);

        await base.GenerateProjectAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
    }

    protected override void AddProjectContent(XElement project, BuildPartition buildPartition, ArtifactsPaths artifactsPaths, FileInfo projectFile)
    {
        base.AddProjectContent(project, buildPartition, artifactsPaths, projectFile);

        project.Add(new XElement("PropertyGroup",
            new XElement("RuntimeIdentifier", settings.RuntimeIdentifier),
            new XElement("PublishAot", "true"),
            new XElement("IlcOptimizationPreference", settings.OptimizationPreference),
            new XElement("OptimizationPreference", settings.OptimizationPreference),
            new XElement("IlcGenerateStackTraceData", settings.GenerateStackTraceData),
            new XElement("StackTraceSupport", settings.GenerateStackTraceData),
            new XComment(" workaround for 'This runtime may not be supported by .NET Core.' error "),
            new XElement("EnsureNETCoreAppRuntime", "false"),
            GetInstructionSetSettings(buildPartition)));

        if (settings.IlCompilerVersion.IsNotBlank())
        {
            project.Add(new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Microsoft.DotNet.ILCompiler"),
                    new XAttribute("Version", settings.IlCompilerVersion))));
        }

        project.Add(new XElement("ItemGroup",
            GetRdXmlFiles(projectFile)
                .Select(file => new XElement("RdXmlFile", new XAttribute("Include", file)))));
    }

    private XElement? GetInstructionSetSettings(BuildPartition buildPartition)
    {
        string instructionSet = settings.InstructionSet.IsBlank()
            ? GetCurrentInstructionSet(buildPartition.Platform)
            : settings.InstructionSet;

        return instructionSet.IsNotBlank()
            ? new XElement("IlcInstructionSet", instructionSet)
            : null;
    }

    public IEnumerable<string> GetRdXmlFiles(FileInfo projectFile)
    {
        yield return GeneratedRdXmlFileName;

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
        var directives = new XElement("Directives",
            new XElement("Application",
                RequiredType("System.Runtime", "System.GC"),
                RequiredType("System.Threading.ThreadPool", "System.Threading.ThreadPool"),
                RequiredType("System.Threading", "System.Threading.Monitor")));

        string directoryName = Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!;
        if (directoryName == null)
            throw new InvalidOperationException($"Can't get directory of projectFilePath ('{artifactsPaths.ProjectFilePath}')");

        return SaveXmlAsync(new XDocument(directives), Path.Combine(directoryName, GeneratedRdXmlFileName), cancellationToken);

        static XElement RequiredType(string assemblyName, string typeName)
            => new("Assembly",
                new XAttribute("Name", assemblyName),
                new XElement("Type",
                    new XAttribute("Name", typeName),
                    new XAttribute("Dynamic", "Required All")));
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
