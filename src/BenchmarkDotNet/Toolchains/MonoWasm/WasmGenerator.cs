using System.IO;
using System.Text;
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
        private readonly bool Aot;
        private readonly string MainJS;

        public WasmGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, bool aot)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion: null)
        {
            Aot = aot;
            CustomRuntimePack = customRuntimePack;
            MainJS = (targetFrameworkMoniker == "net5.0" || targetFrameworkMoniker == "net6.0") ? "main.js" : "test-main.js";
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            if (((WasmRuntime)buildPartition.Runtime).Aot)
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: true, logger);

                var linkDescriptionFileName = "WasmLinkerDescription.xml";
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(artifactsPaths.ProjectFilePath), linkDescriptionFileName), ResourceHelper.LoadTemplate(linkDescriptionFileName));
            } else
            {
                GenerateProjectFile(buildPartition, artifactsPaths, aot: false, logger);
            }
        }

        protected void GenerateProjectFile(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, bool aot, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            WasmRuntime runtime = (WasmRuntime)buildPartition.Runtime;

            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                var (customProperties, sdkName) = GetSettingsThatNeedsToBeCopied(file, projectFile);

                string content = new StringBuilder(ResourceHelper.LoadTemplate("WasmCsProj.txt"))
                    .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                    .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                    .Replace("$RUN_AOT$", aot.ToString().ToLower())
                    .Replace("$CSPROJPATH$", projectFile.FullName)
                    .Replace("$TFM$", TargetFrameworkMoniker)
                    .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                    .Replace("$COPIEDSETTINGS$", customProperties)
                    .Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration)
                    .Replace("$SDKNAME$", sdkName)
                    .Replace("$WASMDATADIR$", runtime.WasmDataDir)
                    .Replace("$TARGET$", CustomRuntimePack != null ? "PublishWithCustomRuntimePack" : "Publish")
                .ToString();

                File.WriteAllText(artifactsPaths.ProjectFilePath, content);
            }
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, MainJS);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        {
                return Path.Combine(buildArtifactsDirectoryPath, "bin", TargetFrameworkMoniker, "browser-wasm", "AppBundle");
        }
    }
}
