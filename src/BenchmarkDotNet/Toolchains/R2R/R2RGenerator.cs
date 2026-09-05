using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using System.Text;
using System.Xml;

namespace BenchmarkDotNet.Toolchains.R2R
{
    internal sealed class R2RGenerator : CsProjGenerator
    {
        private readonly R2RSettings settings;

        public R2RGenerator(R2RSettings settings) : base(settings)
        {
            this.settings = settings;
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
                .Replace("$TFM$", Settings.TargetFrameworkMoniker)
                .Replace("$PROGRAMNAME$", artifactsPaths.ProgramName)
                .Replace("$COPIEDSETTINGS$", customProperties)
                .Replace("$SDKNAME$", sdkName)
                .Replace("$RUNTIMEPACK$", settings.CustomRuntimePack?.FullName)
                .Replace("$CROSSGEN2PACK$", settings.Crossgen2Pack?.FullName)
                .Replace("$RUNTIMEIDENTIFIER$", RuntimeInformation.GetPortableRuntimeIdentifier())
                .ToString();

            await File.WriteAllTextAsync(artifactsPaths.ProjectFilePath, content, cancellationToken).ConfigureAwait(false);

            await GatherReferencesAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
        }

        protected override string GetExecutableExtension() => OsDetector.ExecutableExtension;

        protected override string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
            => Path.Combine(buildArtifactsDirectoryPath, "bin", configuration, Settings.TargetFrameworkMoniker, RuntimeInformation.GetPortableRuntimeIdentifier(), "publish");
    }
}
