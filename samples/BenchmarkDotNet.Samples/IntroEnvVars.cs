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
            public ConfigWithCustomEnvVars()
            {
                AddJob(Job.Default.WithRuntime(CoreRuntime.Core80).WithId("Inlining enabled"));
                AddJob(Job.Default.WithRuntime(CoreRuntime.Core80)
                    .WithEnvironmentVariables([
                        new EnvironmentVariable("DOTNET_JitNoInline", "1"),
                        new EnvironmentVariable("COMPlus_JitNoInline", "1")
                    ])
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
