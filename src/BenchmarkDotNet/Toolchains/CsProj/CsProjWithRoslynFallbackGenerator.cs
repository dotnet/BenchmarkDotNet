using System;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    internal class CsProjWithRoslynFallbackGenerator : IGenerator, IEquatable<CsProjWithRoslynFallbackGenerator>
    {
        private readonly IGenerator csProjGenerator;
        private readonly IGenerator roslynGenerator;

        internal CsProjWithRoslynFallbackGenerator(IGenerator csProjGenerator, IGenerator roslynGenerator)
        {
            this.csProjGenerator = csProjGenerator;
            this.roslynGenerator = roslynGenerator;
        }

        public GenerateResult GenerateProject(BuildPartition buildPartition, ILogger logger, string rootArtifactsFolderPath)
            // Unfortunately we have to generate both at the same time, because the generate and build steps are separated.
            => new CsProjWithRoslynFallbackGenerateResult(
                csProjGenerator.GenerateProject(buildPartition, logger, rootArtifactsFolderPath),
                roslynGenerator.GenerateProject(buildPartition, logger, rootArtifactsFolderPath)
            );

        public override bool Equals(object obj)
            => obj is CsProjWithRoslynFallbackGenerator other && Equals(other);

        public bool Equals(CsProjWithRoslynFallbackGenerator other)
            => csProjGenerator.Equals(other.csProjGenerator)
            && roslynGenerator.Equals(other.roslynGenerator);

        public override int GetHashCode()
            => HashCode.Combine(csProjGenerator, roslynGenerator);
    }
}