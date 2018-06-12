using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Samples
{
    // *** Attribute Style ***

    [MonoJob("Mono x64", @"C:\Program Files\Mono\bin\mono.exe")]
    [MonoJob("Mono x86", @"C:\Program Files (x86)\Mono\bin\mono.exe")]
    public class IntroCustomMono
    {
        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }

    // *** Object Style ***

    [Config(typeof(Config))]
    public class IntroCustomMonoObjectStyle
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.ShortRun.With(new MonoRuntime(
                    "Mono x64", @"C:\Program Files\Mono\bin\mono.exe")));
                Add(Job.ShortRun.With(new MonoRuntime(
                    "Mono x86", @"C:\Program Files (x86)\Mono\bin\mono.exe")));
            }
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }

    // *** Fluent Config ***

    public class IntroCustomMonoFluentConfig
    {
        public static void Run()
        {
            BenchmarkRunner.Run<IntroCustomMonoFluentConfig>(ManualConfig
                .CreateEmpty()
                .With(Job.ShortRun.With(new MonoRuntime(
                    "Mono x64", @"C:\Program Files\Mono\bin\mono.exe")))
                .With(Job.ShortRun.With(new MonoRuntime(
                    "Mono x86", @"C:\Program Files (x86)\Mono\bin\mono.exe"))));
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}