using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Toolchains.MonoWasm
{
    public class WasmGenerator : CsProjGenerator
    {
        private readonly string CustomRuntimePack;
        private readonly string? MainJsTemplatePath;

        public WasmGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, bool aot, string? mainJsTemplatePath)
            : base(targetFrameworkMoniker, cliPath, packagesPath)
        {
            CustomRuntimePack = customRuntimePack;
            MainJsTemplatePath = mainJsTemplatePath;
            BenchmarkRunCallType = aot ? Code.CodeGenBenchmarkRunCallType.Direct : Code.CodeGenBenchmarkRunCallType.Reflection;
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            var targetMainJsPath = GetExecutablePath(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, "");

            if (buildPartition.Runtime.IsAOT)
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: true, logger, targetMainJsPath);

                var linkDescriptionFileName = "WasmLinkerDescription.xml";
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, linkDescriptionFileName), ResourceHelper.LoadTemplate(linkDescriptionFileName));
            }
            else
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: false, logger: logger, targetMainJsPath);
            }

            GenerateMainJS(buildPartition, targetMainJsPath);
        }

        protected void GenerateProjectFile(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, bool aot, ILogger logger, string targetMainJsPath)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            WasmRuntime runtime = (WasmRuntime)buildPartition.Runtime;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, _) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);
            string sdkName = "Microsoft.NET.Sdk.WebAssembly";

            // For CoreCLR WASM:
            // - UseMonoRuntime=false: resolves CoreCLR runtime pack instead of Mono
            // - WasmBuildNative=false: avoids requiring wasm-tools workload
            // - WasmEnableWebcil=false: CoreCLR doesn't support webcil format
            string coreclrOverrides = runtime.RuntimeFlavor == RuntimeFlavor.Mono
                ? string.Empty
                : @"
  <!-- CoreCLR overrides: use CoreCLR runtime instead of Mono -->
  <PropertyGroup>
    <UseMonoRuntime>false</UseMonoRuntime>
    <WasmBuildNative>false</WasmBuildNative>
    <WasmEnableWebcil>false</WasmEnableWebcil>
  </PropertyGroup>
";

            string content = new StringBuilder(ResourceHelper.LoadTemplate("WasmCsProj.txt"))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$RUN_AOT$", aot.ToString().ToLower())
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$TARGET$", CustomRuntimePack.IsNotBlank() ? "PublishWithCustomRuntimePack" : "Publish")
                .Replace("$MAINJS$", targetMainJsPath)
                .Replace("$CORECLR_OVERRIDES$", coreclrOverrides)
            .ToString();

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);

            GatherReferences(buildPartition, artifactsPaths, logger);
        }

        protected void GenerateMainJS(BuildPartition buildPartition, string targetMainJsPath)
        {
            string content = MainJsTemplatePath is null
                ? ResourceHelper.LoadTemplate("benchmark-main.mjs")
                : File.ReadAllText(Path.Combine(Path.GetDirectoryName(buildPartition.AssemblyLocation)!, MainJsTemplatePath));

            targetMainJsPath.EnsureFolderExists();
            File.WriteAllText(targetMainJsPath, content);
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, "wwwroot", "main.js");

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, "publish");
    }
}
