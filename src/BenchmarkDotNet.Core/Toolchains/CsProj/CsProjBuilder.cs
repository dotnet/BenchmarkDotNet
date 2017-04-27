using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjBuilder : DotNetCliBuilder
    {
        internal override string RestoreCommand => "restore --no-dependencies";

        internal override string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself)
            => $"build --framework {frameworkMoniker} --configuration {Configuration}"
               + (justTheProjectItself ? " --no-dependencies" : string.Empty);

        public CsProjBuilder(string targetFrameworkMoniker) : base(targetFrameworkMoniker)
        {
        }
    }
}