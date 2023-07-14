using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    internal class CsProjWithRoslynFallbackGenerateResult : GenerateResult
    {
        public override ArtifactsPaths ArtifactsPaths
            => csProjGeneratorResult.IsGenerateSuccess ? csProjGeneratorResult.ArtifactsPaths : roslynGeneratorResult.ArtifactsPaths;
        public override bool IsGenerateSuccess
            => csProjGeneratorResult.IsGenerateSuccess || roslynGeneratorResult.IsGenerateSuccess;
        public override Exception GenerateException
            => csProjGeneratorResult.IsGenerateSuccess ? null : roslynGeneratorResult.GenerateException ?? csProjGeneratorResult.GenerateException;
        public override IReadOnlyCollection<string> ArtifactsToCleanup
            => csProjGeneratorResult.ArtifactsToCleanup.Concat(roslynGeneratorResult.ArtifactsToCleanup).ToArray();

        internal readonly GenerateResult csProjGeneratorResult;
        internal readonly GenerateResult roslynGeneratorResult;

        internal CsProjWithRoslynFallbackGenerateResult(GenerateResult csProjGeneratorResult, GenerateResult roslynGeneratorResult)
        {
            this.csProjGeneratorResult = csProjGeneratorResult;
            this.roslynGeneratorResult = roslynGeneratorResult;
        }
    }
}