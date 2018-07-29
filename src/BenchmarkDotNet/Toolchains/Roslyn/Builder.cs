using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OurPlatform = BenchmarkDotNet.Environments.Platform;

namespace BenchmarkDotNet.Toolchains.Roslyn
{
    [PublicAPI]
    public class Builder : IBuilder
    {
        private const string MissingReferenceError = "CS0012";

        private static readonly Lazy<AssemblyMetadata[]> FrameworkAssembliesMetadata = new Lazy<AssemblyMetadata[]>(GetFrameworkAssembliesMetadata);

        [PublicAPI]
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            logger.WriteLineInfo($"BuildScript: {generateResult.ArtifactsPaths.BuildScriptFilePath}");

            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(generateResult.ArtifactsPaths.ProgramCodePath));

            var compilationOptions = new CSharpCompilationOptions(
                outputKind: OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: true,
                platform: GetPlatform(buildPartition.Platform),
                deterministic: true);

            compilationOptions = compilationOptions.WithIgnoreCorLibraryDuplicatedTypes();

            var references = Generator
                .GetAllReferences(buildPartition.RepresentativeBenchmarkCase)
                .Select(assembly => AssemblyMetadata.CreateFromFile(assembly.Location))
                .Concat(FrameworkAssembliesMetadata.Value)
                .Distinct()
                .Select(uniqueMetadata => uniqueMetadata.GetReference());

            var (result, missingReferences) = Build(generateResult, syntaxTree, compilationOptions, references);

            if (result.IsBuildSuccess || !missingReferences.Any())
                return result;

            var withMissingReferences = references.Union(missingReferences.Select(assemblyMetadata => assemblyMetadata.GetReference()));

            return Build(generateResult, syntaxTree, compilationOptions, withMissingReferences).result;
        }

        private static (BuildResult result, AssemblyMetadata[] missingReference) Build(GenerateResult generateResult, SyntaxTree syntaxTree, 
            CSharpCompilationOptions compilationOptions, IEnumerable<PortableExecutableReference> references)
        {
            var compilation = CSharpCompilation
                .Create(assemblyName: Path.GetFileName(generateResult.ArtifactsPaths.ExecutablePath))
                .AddSyntaxTrees(syntaxTree)
                .WithOptions(compilationOptions)
                .AddReferences(references);

            using (var executable = File.Create(generateResult.ArtifactsPaths.ExecutablePath))
            {
                var emitResult = compilation.Emit(executable);

                if (emitResult.Success)
                    return (BuildResult.Success(generateResult), default);

                var compilationErrors = emitResult.Diagnostics
                    .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error)
                    .ToArray();
                
                var errors = new StringBuilder("The build has failed!").AppendLine();
                foreach (var diagnostic in compilationErrors)
                    errors.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");

                var missingReferences = GetMissingReferences(compilationErrors);

                return (BuildResult.Failure(generateResult, new Exception(errors.ToString())), missingReferences);
            }
        }

        private Platform GetPlatform(OurPlatform platform)
        {
            switch (platform)
            {
                case OurPlatform.AnyCpu:
                    return Platform.AnyCpu;
                case OurPlatform.X86:
                    return Platform.X86;
                case OurPlatform.X64:
                    return Platform.X64;
                default:
                    throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
            }
        }

        private static AssemblyMetadata[] GetFrameworkAssembliesMetadata()
            => GetFrameworkAssembliesPaths()
                .Where(File.Exists)
                .Select(AssemblyMetadata.CreateFromFile)
                .ToArray();

        private static string[] GetFrameworkAssembliesPaths()
        {
            string frameworkAssembliesDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (frameworkAssembliesDirectory == null)
                return Array.Empty<string>();

            return new[]
            {
                Path.Combine(frameworkAssembliesDirectory, "mscorlib.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.Core.dll"),
                Path.Combine(frameworkAssembliesDirectory, "System.Runtime.dll")
            };
        }

        private static AssemblyMetadata[] GetMissingReferences(Diagnostic[] compilationErrors)
            => compilationErrors
                    .Where(diagnostic => diagnostic.Id == MissingReferenceError)
                    .Select(GetAssemblyName)
                    .Where(assemblyName => assemblyName != default)
                    .Distinct()
                    .Select(assemblyName => Assembly.Load(new AssemblyName(assemblyName)))
                    .Where(assembly => assembly != default)
                    .Select(assembly => AssemblyMetadata.CreateFromFile(assembly.Location))
                    .ToArray();

        private static string GetAssemblyName(Diagnostic diagnostic)
        {
            if (diagnostic.Id != MissingReferenceError || !diagnostic.GetMessage().Contains("You must add a reference to assembly"))
                return default;

            // there is no nice property which would expose the reference name, so we need to to some parsing..
            // CS0012: The type 'ValueType' is defined in an assembly that is not referenced. You must add a reference to assembly 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
            return diagnostic.GetMessage().Split('\'').SingleOrDefault(text => text.Contains("Version="));
        }
    }
}