using BenchmarkDotNet.Toolchains.DotNetCli;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Toolchains.ProjectJson
{
    /// <summary>
    /// generates project.lock.json that tells compiler where to take dlls and source from
    /// and builds executable and copies all required dll's
    /// </summary>
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