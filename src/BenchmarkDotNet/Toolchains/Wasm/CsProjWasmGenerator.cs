using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Text;
using System.Xml;

namespace BenchmarkDotNet.Toolchains.Wasm
{
    public class CsProjWasmGenerator : CsProjGenerator
    {
        private readonly WasmSettings settings;
        private readonly bool aot;
        private readonly bool useCoreClrRuntime;

        public CsProjWasmGenerator(WasmSettings settings, bool aot, bool useCoreClrRuntime) : base(settings)
        {
            this.settings = settings;
            this.aot = aot;
            this.useCoreClrRuntime = useCoreClrRuntime;
            EntryPointType = Code.CodeGenEntryPointType.Asynchronous;
            BenchmarkRunCallType = aot ? Code.CodeGenBenchmarkRunCallType.Direct : Code.CodeGenBenchmarkRunCallType.Reflection;
        }

        protected override async ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
        {
            var targetMainJsPath = GetExecutablePath(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, "");

            if (aot)
            {
                await GenerateProjectFileAsync(buildPartition, artifactsPaths, aot: true, logger, targetMainJsPath, cancellationToken).ConfigureAwait(false);

                var linkDescriptionFileName = "WasmLinkerDescription.xml";
                await File.WriteAllTextAsync(
                    Path.Combine(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, linkDescriptionFileName),
                    await ResourceHelper.LoadTemplateAsync(linkDescriptionFileName, cancellationToken).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await GenerateProjectFileAsync(buildPartition, artifactsPaths, aot: false, logger: logger, targetMainJsPath, cancellationToken).ConfigureAwait(false);
            }

            await GenerateMainJS(settings.MainJsTemplate, targetMainJsPath, cancellationToken).ConfigureAwait(false);
        }

        protected async ValueTask GenerateProjectFileAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, bool aot, ILogger logger, string targetMainJsPath, CancellationToken cancellationToken)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, _) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);
            string sdkName = "Microsoft.NET.Sdk.WebAssembly";

            // For CoreCLR WASM:
            // - UseMonoRuntime=false: resolves CoreCLR runtime pack instead of Mono
            // - WasmBuildNative=false: avoids requiring wasm-tools workload
            // - WasmEnableWebcil=false: CoreCLR doesn't support webcil format
            string coreclrOverrides = !useCoreClrRuntime
                ? string.Empty
                : """
                  <!-- CoreCLR overrides: use CoreCLR runtime instead of Mono -->
                  <PropertyGroup>
                    <UseMonoRuntime>false</UseMonoRuntime>
                    <WasmBuildNative>false</WasmBuildNative>
                    <WasmEnableWebcil>false</WasmEnableWebcil>
                  </PropertyGroup>

                """;

            string content = new StringBuilder(await ResourceHelper.LoadTemplateAsync("WasmCsProj.txt", cancellationToken).ConfigureAwait(false))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$RUN_AOT$", aot.ToString().ToLower())
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", Settings.TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$MAINJS$", targetMainJsPath)
                .Replace("$CORECLR_OVERRIDES$", coreclrOverrides)
                .ToString();

            await File.WriteAllTextAsync(artifactsPaths.ProjectFilePath, content, cancellationToken).ConfigureAwait(false);

            // Integration tests are built without dependencies, so we skip gathering dlls.
            if (buildPartition.ForcedNoDependenciesForIntegrationTests)
                return;

            await GatherReferencesAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
        }

        protected async ValueTask GenerateMainJS(FileInfo? mainJsTemplate, string targetMainJsPath, CancellationToken cancellationToken)
        {
            string content = mainJsTemplate is null
                ? await ResourceHelper.LoadTemplateAsync("benchmark-main.mjs", cancellationToken).ConfigureAwait(false)
                : await File.ReadAllTextAsync(mainJsTemplate.FullName, cancellationToken).ConfigureAwait(false);

            targetMainJsPath.EnsureFolderExists();
            await File.WriteAllTextAsync(targetMainJsPath, content, cancellationToken).ConfigureAwait(false);
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, "wwwroot", "main.mjs");

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, "publish");
    }
}
