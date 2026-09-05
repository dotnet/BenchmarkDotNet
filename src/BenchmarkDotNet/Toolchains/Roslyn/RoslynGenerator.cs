using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Running;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    public class RoslynGenerator : GeneratorBase
    {
        public static readonly RoslynGenerator Instance = new();

        protected override string GetBuildArtifactsDirectoryPath(BuildPartition buildPartition, string programName)
            => Path.GetDirectoryName(buildPartition.AssemblyLocation)!;

        protected override string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths) =>
        [
            artifactsPaths.ProgramCodePath,
            artifactsPaths.AppConfigPath,
            artifactsPaths.BuildScriptFilePath,
            artifactsPaths.ExecutablePath
        ];

        protected override async ValueTask GenerateBuildScriptAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
        {
            string prefix = OsDetector.IsWindows() ? "" : "#!/bin/bash\n";
            var list = new List<string>();
            if (!OsDetector.IsWindows())
                list.Add("mono");
            list.Add("csc");
            list.Add("/noconfig");
            list.Add("/target:exe");
            list.Add("/optimize");
            list.Add("/unsafe");
            list.Add("/deterministic");
            list.Add("/platform:" + buildPartition.Platform.ToConfig());
            list.Add("/appconfig:" + artifactsPaths.AppConfigPath.EscapeCommandLine());
            var references = GetAllReferences(buildPartition.Benchmarks[0]).Select(assembly => assembly.Location.EscapeCommandLine());
            list.Add("/reference:" + string.Join(",", references));
            list.Add(Path.GetFileName(artifactsPaths.ProgramCodePath));

            await File.WriteAllTextAsync(
                artifactsPaths.BuildScriptFilePath,
                prefix + string.Join(" ", list),
                cancellationToken
            ).ConfigureAwait(false);
        }

        internal static IEnumerable<Assembly> GetAllReferences(BenchmarkBuildInfo buildInfo)
            => buildInfo.BenchmarkCase.Descriptor.Type.GetTypeInfo().Assembly
                .GetReferencedAssemblies()
                .Select(Assembly.Load)
                .Append(buildInfo.BenchmarkCase.Descriptor.Type.GetTypeInfo().Assembly) // This assembly does not have to have a reference to BenchmarkDotNet (e.g. custom framework for benchmarking that internally uses BenchmarkDotNet)
                .Concat(BenchmarkDotNetReferences.Assemblies) // BenchmarkDotNet + Perfolizer
                .Concat(BenchmarkDotNetReferences.Types.Select(type => type.GetTypeInfo().Assembly)) // TaskExtensions (ValueTask)
                // In-process diagnoser handlers
                .Concat(buildInfo.CompositeInProcessDiagnoser.GetHandlerData(buildInfo.BenchmarkCase)
                    .Select(handlerData => handlerData.HandlerType?.GetTypeInfo().Assembly)
                    .WhereNotNull())
                .Distinct();
    }
}