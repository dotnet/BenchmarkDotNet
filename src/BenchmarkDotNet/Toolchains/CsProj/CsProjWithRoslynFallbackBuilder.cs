using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    internal class CsProjWithRoslynFallbackBuilder : IBuilder
    {
        private readonly IBuilder csProjBuilder;
        private readonly IBuilder roslynBuilder;

        internal CsProjWithRoslynFallbackBuilder(IBuilder csProjBuilder, IBuilder roslynBuilder)
        {
            this.csProjBuilder = csProjBuilder;
            this.roslynBuilder = roslynBuilder;
        }

        public BuildResult Build(GenerateResult generateResult, BuildPartition buildPartition, ILogger logger)
        {
            var csProjGenerateResult = (CsProjWithRoslynFallbackGenerateResult) generateResult;
            BuildResult buildResult;
            if (csProjGenerateResult.csProjGeneratorResult.IsGenerateSuccess)
            {
                buildResult = csProjBuilder.Build(csProjGenerateResult.csProjGeneratorResult, buildPartition, logger);
                if (buildResult.IsBuildSuccess)
                {
                    return buildResult;
                }
            }
            return roslynBuilder.Build(csProjGenerateResult.roslynGeneratorResult, buildPartition, logger);
        }
    }
}