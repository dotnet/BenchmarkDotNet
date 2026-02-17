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
        private const string MainJS = "benchmark-main.mjs";

        public WasmGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, bool aot)
            : base(targetFrameworkMoniker, cliPath, packagesPath)
        {
            CustomRuntimePack = customRuntimePack;
            EntryPointType = Code.CodeGenEntryPointType.Asynchronous;
            BenchmarkRunCallType = aot ? Code.CodeGenBenchmarkRunCallType.Direct : Code.CodeGenBenchmarkRunCallType.Reflection;
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            if (((WasmRuntime)buildPartition.Runtime).Aot)
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: true, logger);

                var linkDescriptionFileName = "WasmLinkerDescription.xml";
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, linkDescriptionFileName), ResourceHelper.LoadTemplate(linkDescriptionFileName));
            } else
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: false, logger);
            }
        }

        protected void GenerateProjectFile(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, bool aot, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            WasmRuntime runtime = (WasmRuntime) buildPartition.Runtime;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, _) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);
            // Microsoft.NET.Sdk.WebAssembly auto-defaults UseMonoRuntime=true.
            string sdkName = runtime.IsMonoRuntime ? "Microsoft.NET.Sdk.WebAssembly" : "Microsoft.NET.Sdk";

            string content = new StringBuilder(ResourceHelper.LoadTemplate("WasmCsProj.txt"))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$RUN_AOT$", aot.ToString().ToLower())
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$WASMDATADIR$", runtime.WasmDataDir)
                .Replace("$TARGET$", CustomRuntimePack.IsNotBlank() ? "PublishWithCustomRuntimePack" : "Publish")
            .ToString();

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);

            // Place benchmark-main.mjs in wwwroot/ next to the generated csproj.
            string projectWwwroot = Path.Combine(Path.GetDirectoryName(artifactsPaths.ProjectFilePath)!, "wwwroot");
            Directory.CreateDirectory(projectWwwroot);
            File.WriteAllText(Path.Combine(projectWwwroot, MainJS), ResourceHelper.LoadTemplate(MainJS));

            GatherReferences(buildPartition, artifactsPaths, logger);
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, "wwwroot", MainJS);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, "browser-wasm");
    }
}
