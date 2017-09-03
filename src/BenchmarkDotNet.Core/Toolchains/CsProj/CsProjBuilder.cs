using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjBuilder : DotNetCliBuilder
    {
        internal override string RestoreCommand => "restore --no-dependencies";

        internal override string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself, string configuration)
            => $"build --framework {frameworkMoniker} --configuration {configuration}"
               + (justTheProjectItself ? " --no-dependencies" : string.Empty);

        public CsProjBuilder(string targetFrameworkMoniker) : base(targetFrameworkMoniker)
        {
        }
    }
}