using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.ProjectJson
{
    [PublicAPI]
    public class ProjectJsonBuilder : DotNetCliBuilder
    {
        internal const string OutputDirectory = "binaries";

        internal override string RestoreCommand => "restore";

        internal override string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself)
            => $"build --framework {frameworkMoniker} --configuration {Configuration} --output {OutputDirectory}"
               + (justTheProjectItself ? " --no-dependencies" : string.Empty);

        public ProjectJsonBuilder(string targetFrameworkMoniker) : base(targetFrameworkMoniker)
        {
        }
    }
}