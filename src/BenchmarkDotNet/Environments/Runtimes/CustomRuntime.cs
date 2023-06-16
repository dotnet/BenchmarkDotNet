using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public abstract class CustomRuntime : Runtime
    {
        protected CustomRuntime(RuntimeMoniker runtimeMoniker, string msBuildMoniker, string displayName)
            : base(runtimeMoniker, msBuildMoniker, displayName)
        {
        }
    }
}