using BenchmarkDotNet.Code;
using BenchmarkDotNet.Detectors;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using System.Text;

namespace BenchmarkDotNet.Toolchains;

public abstract class BuilderBase : IBuilder
{
    /// <inheritdoc cref="CodeGenEntryPointType"/>
    public CodeGenEntryPointType EntryPointType { get; init; }

    /// <inheritdoc cref="CodeGenBenchmarkRunCallType"/>
    public CodeGenBenchmarkRunCallType BenchmarkRunCallType { get; init; }

    public virtual bool GetSupportsConcurrency(BuildPartition buildPartition) => true;

    public async ValueTask<BuildResult> BuildAsync(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath, CancellationToken cancellationToken)
    {
        await ThreadPoolHelper.Switch(cancellationToken);

        // Generating runs a build of its own to gather the references, so both halves are guarded the same way.
        var artifactsPaths = ArtifactsPaths.Empty;
        BuildResult buildResult;
        try
        {
            artifactsPaths = GetArtifactsPaths(buildPartition, rootArtifactsFolderPath);

            await GenerateAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);

            buildResult = await BuildAsync(artifactsPaths, buildPartition, logger, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
        {
            buildResult = BuildResult.Failure(artifactsPaths, ex);
        }

        // Attached here rather than by every builder, so that what was generated is cleaned up however it failed.
        return buildResult.WithArtifactsToCleanup(GetArtifactsToCleanup(artifactsPaths));
    }

    [PublicAPI] protected abstract ValueTask<BuildResult> BuildAsync(ArtifactsPaths artifactsPaths, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken);

    private async ValueTask GenerateAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken)
    {
        // Cleanup artifact files/directories before generating project files.
        foreach (var path in GetArtifactsToCleanup(artifactsPaths))
            FileCleanupHelper.Cleanup(path);

        // There is no async file copy API, so we just do it synchronously. We are likely on a ThreadPool thread here anyway if this builder is ran in parallel.
        CopyAllRequiredFiles(artifactsPaths);

        await GenerateCodeAsync(buildPartition, artifactsPaths, cancellationToken).ConfigureAwait(false);
        await GenerateAppConfigAsync(buildPartition, artifactsPaths, cancellationToken).ConfigureAwait(false);
        await GenerateNuGetConfigAsync(artifactsPaths, cancellationToken).ConfigureAwait(false);
        await GenerateProjectAsync(buildPartition, artifactsPaths, logger, cancellationToken).ConfigureAwait(false);
        await GenerateBuildScriptAsync(buildPartition, artifactsPaths, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// returns a path to the folder where auto-generated project and code are going to be placed
    /// </summary>
    [PublicAPI] protected abstract string GetBuildArtifactsDirectoryPath(BuildPartition assemblyLocation, string programName);

    /// <summary>
    /// returns a path where executable should be found after the build (usually \bin)
    /// </summary>
    [PublicAPI]
    protected virtual string GetBinariesDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        => buildArtifactsDirectoryPath;

    /// <summary>
    /// returns a path where the publish directory should be found after the build (usually \publish)
    /// </summary>
    [PublicAPI]
    protected virtual string GetPublishDirectoryPath(string buildArtifactsDirectoryPath, string configuration)
        => Path.Combine(buildArtifactsDirectoryPath, "publish");

    /// <summary>
    /// returns OS-specific executable extension
    /// </summary>
    [PublicAPI]
    protected virtual string GetExecutableExtension()
        => OsDetector.ExecutableExtension;

    /// <summary>
    /// returns the extension of the file the generated boilerplate code is written to
    /// <remarks>It is not ".cs" by default to stop VS from displaying it and xprojs/csprojs that include all *.cs from compiling it.</remarks>
    /// </summary>
    [PublicAPI]
    protected virtual string GetCodeFileExtension() => ".notcs";

    /// <summary>
    /// returns a path to the auto-generated .csproj file
    /// </summary>
    [PublicAPI]
    protected virtual string GetProjectFilePath(string buildArtifactsDirectoryPath)
        => string.Empty;

    /// <summary>
    /// returns a list of artifacts that should be removed after running the benchmarks
    /// </summary>
    [PublicAPI] protected abstract string[] GetArtifactsToCleanup(ArtifactsPaths artifactsPaths);

    /// <summary>
    /// if you need to copy some extra files to make the benchmarks work you should override this method
    /// </summary>
    [PublicAPI] protected virtual void CopyAllRequiredFiles(ArtifactsPaths artifactsPaths) { }

    /// <summary>
    /// generates NuGet.Config file to make sure that BDN is using the right NuGet feeds
    /// </summary>
    [PublicAPI] protected virtual ValueTask GenerateNuGetConfigAsync(ArtifactsPaths artifactsPaths, CancellationToken cancellationToken) => new();

    /// <summary>
    /// generates .csproj file with a reference to the project with benchmarks
    /// </summary>
    [PublicAPI] protected virtual ValueTask GenerateProjectAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, ILogger logger, CancellationToken cancellationToken) => new();

    /// <summary>
    /// generates a script can be used when debugging compilation issues
    /// </summary>
    [PublicAPI] protected abstract ValueTask GenerateBuildScriptAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken);

    /// <summary>
    /// returns a path to the folder where NuGet packages should be restored
    /// </summary>
    [PublicAPI] protected virtual string GetPackagesDirectoryPath(string buildArtifactsDirectoryPath) => "";

    /// <summary>
    /// generates an app.config file next to the executable with benchmarks
    /// </summary>
    [PublicAPI]
    protected virtual async ValueTask GenerateAppConfigAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
    {
        string sourcePath = $"{buildPartition.AssemblyLocation}.config";
        artifactsPaths.AppConfigPath.EnsureFolderExists();

        using var source = File.Exists(sourcePath) ? new StreamReader(File.OpenRead(sourcePath)) : TextReader.Null;
        using var destination = new StreamWriter(File.Create(artifactsPaths.AppConfigPath), Encoding.UTF8);
        await AppConfigGenerator.GenerateAsync(buildPartition.RepresentativeBenchmarkCase.Job, source, destination, buildPartition.Resolver, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// generates the C# source code with all required boilerplate.
    /// <remarks>You most probably do NOT need to override this method!!</remarks>
    /// </summary>
    [PublicAPI]
    protected virtual async ValueTask GenerateCodeAsync(BuildPartition buildPartition, ArtifactsPaths artifactsPaths, CancellationToken cancellationToken)
        => await File.WriteAllTextAsync(
            artifactsPaths.ProgramCodePath,
            await CodeGenerator.GenerateAsync(buildPartition, EntryPointType, BenchmarkRunCallType, cancellationToken).ConfigureAwait(false),
            cancellationToken)
            .ConfigureAwait(false);

    protected virtual string GetExecutablePath(string binariesDirectoryPath, string programName) => Path.Combine(binariesDirectoryPath, $"{programName}{GetExecutableExtension()}");

    private ArtifactsPaths GetArtifactsPaths(BuildPartition buildPartition, string rootArtifactsFolderPath)
    {
        string programName = buildPartition.ProgramName;
        string buildArtifactsDirectoryPath = GetBuildArtifactsDirectoryPath(buildPartition, programName);
        string binariesDirectoryPath = GetBinariesDirectoryPath(buildArtifactsDirectoryPath, buildPartition.BuildConfiguration);

        string executablePath = GetExecutablePath(binariesDirectoryPath, programName);

        return new ArtifactsPaths(
            rootArtifactsFolderPath: rootArtifactsFolderPath,
            buildArtifactsDirectoryPath: buildArtifactsDirectoryPath,
            binariesDirectoryPath: binariesDirectoryPath,
            publishDirectoryPath: GetPublishDirectoryPath(buildArtifactsDirectoryPath, buildPartition.BuildConfiguration),
            programCodePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{GetCodeFileExtension()}"),
            appConfigPath: $"{executablePath}.config",
            nuGetConfigPath: Path.Combine(buildArtifactsDirectoryPath, "NuGet.config"),
            projectFilePath: GetProjectFilePath(buildArtifactsDirectoryPath),
            buildScriptFilePath: Path.Combine(buildArtifactsDirectoryPath, $"{programName}{OsDetector.ScriptFileExtension}"),
            executablePath: executablePath,
            programName: programName,
            packagesDirectoryName: GetPackagesDirectoryPath(buildArtifactsDirectoryPath));
    }
}