using BenchmarkDotNet.Toolchains.DotNetCli;

namespace BenchmarkDotNet.Toolchains.CustomCoreClr
{
    public class Publisher : DotNetCliBuilder
    {
        public Publisher(string targetFrameworkMoniker, string customDotNetCliPath = null) 
            : base(targetFrameworkMoniker, customDotNetCliPath)
        {
        }

        internal override string RestoreCommand => null; // don't run restore, dotnet publish will

        internal override string GetBuildCommand(string frameworkMoniker, bool justTheProjectItself, string configuration)
            => $"publish -c {configuration}";
    }
}
