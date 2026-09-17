using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Toolchains.Results;

[PublicAPI]
public class BuildResult
{
    public ArtifactsPaths ArtifactsPaths { get; }

    /// <summary>
    /// The files and directories the run deletes afterwards, unless it was told to keep them.
    /// </summary>
    public IReadOnlyCollection<string> ArtifactsToCleanup { get; private init; } = [];

    public bool IsBuildSuccess { get; }

    public string ErrorMessage { get; }

    /// <summary>
    /// Set when the build ended in an exception rather than in an error the build itself reported.
    /// </summary>
    public Exception? Exception { get; }

    private BuildResult(ArtifactsPaths artifactsPaths, bool isBuildSuccess, string errorMessage, Exception? exception)
    {
        ArtifactsPaths = artifactsPaths;
        IsBuildSuccess = isBuildSuccess;
        ErrorMessage = errorMessage;
        Exception = exception;
    }

    public static BuildResult Success(ArtifactsPaths artifactsPaths)
        => new(artifactsPaths, true, errorMessage: "", exception: null);

    public static BuildResult Failure(ArtifactsPaths artifactsPaths, string errorMessage)
        => new(artifactsPaths, false, errorMessage, exception: null);

    public static BuildResult Failure(ArtifactsPaths artifactsPaths, Exception exception)
        => new(artifactsPaths, false,
            $"Exception! {Environment.NewLine}Message: {exception.Message},{Environment.NewLine}Stack trace:{Environment.NewLine}{exception.StackTrace}",
            exception);

    /// <summary>
    /// Returns the result with the artifacts to clean up attached. <see cref="BuilderBase"/> calls this for
    /// every result it returns, so that its builders do not have to carry the list through their own failure
    /// paths; a builder that implements <see cref="IBuilder"/> directly calls it itself.
    /// </summary>
    public BuildResult WithArtifactsToCleanup(IReadOnlyCollection<string> artifactsToCleanup)
        => new(ArtifactsPaths, IsBuildSuccess, ErrorMessage, Exception) { ArtifactsToCleanup = artifactsToCleanup };

    public override string ToString() => "BuildResult: " + (IsBuildSuccess ? "Success" : "Failure");

    internal bool TryToExplainFailureReason(IReadOnlyList<Type> inProcessDiagnoserHandlerTypes, [NotNullWhen(true)] out string? reason)
        => MsBuildErrorMapper.TryToExplainFailureReason(this, inProcessDiagnoserHandlerTypes, out reason);
}
