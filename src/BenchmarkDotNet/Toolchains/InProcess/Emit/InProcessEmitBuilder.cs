using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Results;
using System.Reflection;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitBuilder : IBuilder
    {
        public async ValueTask<BuildResult> BuildAsync(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger, CancellationToken cancellationToken)
        {
            Assembly? assembly = null;
            Exception? buildError = null;
            try
            {
                assembly = RunnableEmitter.EmitPartitionAssembly(generateResult, buildPartition, logger);
            }
            catch (Exception ex) when (!ExceptionHelper.IsProperCancelation(ex, cancellationToken))
            {
                buildError = ex;
            }

            if (buildError != null)
                return BuildResult.Failure(generateResult, buildError);

            // HACK: use custom artifacts path class to pass the generated assembly.
            return BuildResult.Success(
                GenerateResult.Success(
                    new InProcessEmitArtifactsPath(assembly!, generateResult.ArtifactsPaths),
                    generateResult.ArtifactsToCleanup));
        }
    }
}