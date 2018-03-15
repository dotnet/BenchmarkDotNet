using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.CsProj
{
    [PublicAPI]
    public class CsProjBuilder : DotNetCliBuilder
    {
        public override string RestoreCommand => "restore --no-dependencies";

        public override string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself, string configuration)
            => $"build --framework {frameworkMoniker} --configuration {configuration} --no-restore"
               + (justTheProjectItself ? " --no-dependencies" : string.Empty);

        public CsProjBuilder(string targetFrameworkMoniker, string customDotNetCliPath) 
            : base(targetFrameworkMoniker, customDotNetCliPath)
        {
        }
    }
}
