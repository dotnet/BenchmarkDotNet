using System.IO;
using System.Text;
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

        public WasmGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion: null)
        {
            CustomRuntimePack = customRuntimePack;
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);
            using (var file = new StreamReader(File.OpenRead(projectFile.FullName)))
            {
                var (customProperties, sdkName) = GetSettingsThatNeedsToBeCopied(file, projectFile);

                string content = new StringBuilder(ResourceHelper.LoadTemplate("WasmCsProj.txt"))
                    .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                    .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                    .Replace("$CSPROJPATH$", projectFile.FullName)
                    .Replace("$TFM$", TargetFrameworkMoniker)
                    .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                    .Replace("$COPIEDSETTINGS$", customProperties)
                    .Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration)
                    .Replace("$SDKNAME$", sdkName)
                    .Replace("$RUNTIMEPACK$", CustomRuntimePack ?? "")
                    .Replace("$TARGET$", CustomRuntimePack != null ? "PublishWithCustomRuntimePack" : "Publish")
                    .ToString();

                File.WriteAllText(artifactsPaths.ProjectFilePath, content);
            }
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, "runtime.js");

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", TargetFrameworkMoniker, "browser-wasm", "publish", "output");
    }
}
