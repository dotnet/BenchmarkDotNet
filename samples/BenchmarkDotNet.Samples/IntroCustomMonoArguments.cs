using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(ConfigWithCustomArguments))]
    public class IntroCustomMonoArguments
    {
        public class ConfigWithCustomArguments : ManualConfig
        {
            public ConfigWithCustomArguments()
            {
                // --optimize=MODE , -O=mode
                // MODE is a comma separated list of optimizations. They also allow
                // optimizations to be turned off by prefixing the optimization
                // name with a minus sign.

                AddJob(Job.Default
                    .WithRuntime(MonoRuntime.Default)
                    .WithArguments(new[] { new MonoArgument("--optimize=inline") })
                    .WithId("Inlining enabled"));
                AddJob(Job.Default
                    .WithRuntime(MonoRuntime.Default)
                    .WithArguments(new[] { new MonoArgument("--optimize=-inline") })
                    .WithId("Inlining disabled"));
            }
        }

        [Benchmark]
        public void Sample()
        {
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
        }

        private void ShouldGetInlined() { }
    }
}