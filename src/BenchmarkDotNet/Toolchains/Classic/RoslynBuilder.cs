#if CLASSIC
using System.IO;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace BenchmarkDotNet.Toolchains.Classic
{
    internal class RoslynBuilder : IBuilder
    {
        private static readonly Lazy<AssemblyMetadata[]> FrameworkAssembliesMetadata = new Lazy<AssemblyMetadata[]>(GetFrameworkAssembliesMetadata);

        public BuildResult Build(GenerateResult generateResult, ILogger logger, Benchmark benchmark)
        {
            logger.WriteLineInfo($"BuildScript: {generateResult.ArtifactsPaths.BuildScriptFilePath}");

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(generateResult.ArtifactsPaths.ProgramCodePath));

            var compilationOptions = new CSharpCompilationOptions(
                 outputKind: OutputKind.ConsoleApplication,
                 optimizationLevel: OptimizationLevel.Release,
                 allowUnsafe: true,
                 platform: GetPlatform(benchmark.Job.Platform),
                 deterministic: true);

            var references = RoslynGenerator
                .GetAllReferences(benchmark)
                .Select(assembly => AssemblyMetadata.CreateFromFile(assembly.Location))
                .Concat(FrameworkAssembliesMetadata.Value)
                .Distinct()
                .Select(uniqueMetadata => uniqueMetadata.GetReference());

            var compilation = CSharpCompilation
                .Create(assemblyName: Path.GetFileName(generateResult.ArtifactsPaths.ExecutablePath))
                .AddSyntaxTrees(syntaxTree)
                .WithOptions(compilationOptions)
                .AddReferences(references);

            using (var executable = File.Create(generateResult.ArtifactsPaths.ExecutablePath))
            {
                var emitResult = compilation.Emit(executable);

                if(emitResult.Success)
                {
                    return BuildResult.Success(generateResult);
                }

                foreach (var diagnostic in emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error))
                {
                    logger.WriteError($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                return BuildResult.Failure(generateResult);
            }
        }

        private Platform GetPlatform(Jobs.Platform platform)
        {
            switch(platform)
            {
                case Jobs.Platform.Host:
                    return IntPtr.Size == 4 ? Platform.X86 : Platform.X64;
                case Jobs.Platform.AnyCpu:
                    return Platform.AnyCpu;
                case Jobs.Platform.X86:
                    return Platform.X86;
                case Jobs.Platform.X64:
                    return Platform.X64;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }

        private static AssemblyMetadata[] GetFrameworkAssembliesMetadata()
        {
            return GetFrameworkAssembliesPaths()
                .Where(File.Exists)
                .Select(AssemblyMetadata.CreateFromFile)
                .ToArray();
        }

        private static string[] GetFrameworkAssembliesPaths()
        {
            var frameworkAssembliesDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (frameworkAssembliesDirectory == null)
            {
                return new string[0];
            }

            return new[]
            {
                Path.Combine(frameworkAssembliesDirectory, "mscorlib.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.Core.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.Runtime.dll")
            };
        }
    }
}
#endif