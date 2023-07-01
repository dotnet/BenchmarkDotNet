using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.MonoAotLLVM
{
    public class MonoAotLLVMGenerator : CsProjGenerator
    {
        private readonly string CustomRuntimePack;
        private readonly string AotCompilerPath;
        private readonly MonoAotCompilerMode AotCompilerMode;

        public MonoAotLLVMGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, string aotCompilerPath, MonoAotCompilerMode aotCompilerMode)
            : base(targetFrameworkMoniker, cliPath, packagesPath, runtimeFrameworkVersion: null)
        {
            CustomRuntimePack = customRuntimePack;
            AotCompilerPath = aotCompilerPath;
            AotCompilerMode = aotCompilerMode;
        }

        protected override void GenerateProject(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            string useLLVM = AotCompilerMode == MonoAotCompilerMode.llvm ? "true" : "false";

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, sdkName) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);

            string content = new StringBuilder(ResourceHelper.LoadTemplate("MonoAOTLLVMCsProj.txt"))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$CONFIGURATIONNAME$", buildPartition.BuildConfiguration)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$RUNTIMEPACK$", CustomRuntimePack ?? "")
                .Replace("$COMPILERBINARYPATH$", AotCompilerPath)
                .Replace("$RUNTIMEIDENTIFIER$", CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier())
                .Replace("$USELLVM$", useLLVM)
                .ToString();

            File.WriteAllText(artifactsPaths.ProjectFilePath, content);
        }

        protected override string GetExecutablePath(string binariesDirectoryPath, string programName)
            => Portability.RuntimeInformation.IsWindows()
                ? Path.Combine(binariesDirectoryPath, $"{programName}.exe")
                : Path.Combine(binariesDirectoryPath, programName);

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", TargetFrameworkMoniker, CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier(), "publish");
    }
}
