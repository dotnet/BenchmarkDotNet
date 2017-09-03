using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples.Intro
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
                // optimizations to be turned off by prefixing the optimization name with a minus sign.

                Add(Job.Mono.With(new[] { new MonoArgument("--optimize=inline") }).WithId("Inlining enabled"));
                Add(Job.Mono.With(new[] { new MonoArgument("--optimize=-inline") }).WithId("Inlining disabled"));
            }
        }

        [Benchmark]
        public void Sample()
        {
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
            ShouldGetInlined(); ShouldGetInlined(); ShouldGetInlined();
        }

        void ShouldGetInlined() { }
    }
}