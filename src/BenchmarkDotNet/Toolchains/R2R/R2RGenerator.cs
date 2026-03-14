using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.R2R
{
    public class R2RGenerator : CsProjGenerator
    {
        private readonly string CustomRuntimePack;
        private readonly string Crossgen2Pack;

        public R2RGenerator(string targetFrameworkMoniker, string cliPath, string packagesPath, string customRuntimePack, string crossgen2Pack)
            : base(targetFrameworkMoniker, cliPath, packagesPath)
        {
            CustomRuntimePack = customRuntimePack;
            Crossgen2Pack = crossgen2Pack;
            BenchmarkRunCallType = Code.CodeGenBenchmarkRunCallType.Direct;
        }

        protected override async ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
        {
            BenchmarkCase benchmark = buildPartition.RepresentativeBenchmarkCase;
            var projectFile = GetProjectFilePath(benchmark.Descriptor.Type, logger);

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile.FullName);
            var (customProperties, sdkName) = GetSettingsThatNeedToBeCopied(xmlDoc, projectFile);

            string content = new StringBuilder(await ResourceHelper.LoadTemplateAsync("R2RCsProj.txt", cancellationToken).ConfigureAwait(false))
                .Replace("$PLATFORM$", buildPartition.Platform.ToConfig())
                .Replace("$CODEFILENAME$", Path.GetFileName(artifactsPaths.ProgramCodePath))
                .Replace("$CSPROJPATH$", projectFile.FullName)
                .Replace("$TFM$", TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$RUNTIMEPACK$", CustomRuntimePack)
                .Replace("$CROSSGEN2PACK$", Crossgen2Pack)
                .Replace("$RUNTIMEIDENTIFIER$", CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier())
                .ToString();

            await File.WriteAllTextAsync(artifactsPaths.ProjectFilePath, content, cancellationToken).ConfigureAwait(false);

            await GatherReferencesAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
        }

        protected override string GetExecutableExtension() => OsDetector.ExecutableExtension;

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, TargetFrameworkMoniker, CustomDotNetCliToolchainBuilder.GetPortableRuntimeIdentifier(), "publish");
    }
}
