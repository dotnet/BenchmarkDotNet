using System;
using System.Reflection;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit
{
    public class InProcessEmitBuilder : IBuilder
    {
        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            Assembly assembly = null;
            Exception buildError = null;
            try
            {
                assembly = RunnableEmitter.EmitPartitionAssembly(generateResult, buildPartition, logger);
            }
            catch (Exception ex)
            {
                buildError = ex;
            }

            if (buildError != null)
                return BuildResult.Failure(generateResult, buildError);

            // HACK: use custom artifacts path class to pass the generated assembly.
            return BuildResult.Success(
                GenerateResult.Success(
                    new InProcessEmitArtifactsPath(assembly, generateResult.ArtifactsPaths),
                    generateResult.ArtifactsToCleanup));
        }
    }
}