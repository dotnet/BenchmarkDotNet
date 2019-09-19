using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(ConfigWithCustomEnvVars))]
    public class IntroEnvVars
    {
        private class ConfigWithCustomEnvVars : ManualConfig
        {
            private const string JitNoInline = "COMPlus_JitNoInline";
            
            public ConfigWithCustomEnvVars()
            {
                Add(Job.Default.With(CoreRuntime.Core21).WithId("Inlining enabled"));
                Add(Job.Default.With(CoreRuntime.Core21)
                    .With(new[] { new EnvironmentVariable(JitNoInline, "1") })
                    .WithId("Inlining disabled"));
            }
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}