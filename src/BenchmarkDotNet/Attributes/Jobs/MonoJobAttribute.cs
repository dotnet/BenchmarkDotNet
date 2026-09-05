using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.Mono;

namespace BenchmarkDotNet.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MonoJobAttribute : JobConfigBaseAttribute
    {
        public MonoJobAttribute(bool baseline = false) : base(Job.Default.WithRuntime(MonoRuntime.Default).WithBaseline(baseline))
        {
        }

        public MonoJobAttribute(string runtimeMoniker, bool baseline = false) : base(Job.Default.WithRuntime(Runtime.Parse(runtimeMoniker)).WithBaseline(baseline))
        {
        }

        public MonoJobAttribute(string name, string path, bool baseline = false)
            : base(new Job(name).WithToolchain(RoslynMonoToolchain.From(new() { MonoPath = new(path) })).WithBaseline(baseline).Freeze())
        {
        }
    }
}