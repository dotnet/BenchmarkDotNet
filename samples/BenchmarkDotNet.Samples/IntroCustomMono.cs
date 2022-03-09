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
                AddJob(Job.ShortRun.WithRuntime(new MonoRuntime(
                    "Mono x64", @"C:\Program Files\Mono\bin\mono.exe")));
                AddJob(Job.ShortRun.WithRuntime(new MonoRuntime(
                    "Mono x86", @"C:\Program Files (x86)\Mono\bin\mono.exe")));
            }
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }

    // ** Object Style, Using AOT **

    [Config(typeof(Config))]
    public class IntroCustomMonoObjectStyleAot
    {
        private class Config : ManualConfig
        {
            public void AddMono (string name, string mono_top_dir)
            {
                var aot_compile_args  = "--aot=llvm";
                var mono_bcl = $@"{mono_top_dir}\lib\mono\4.5";
                var mono_bin = $@"{mono_top_dir}\bin\mono.exe";
                AddJob(Job.ShortRun.WithRuntime(new MonoRuntime(
                    name, mono_bin, aot_compile_args, mono_bcl)));
            }

            public Config()
            {
                AddMono("Mono x64", @"C:\Program Files\Mono");
                AddMono("Mono x86", @"C:\Program Files (x86)\Mono");
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
                .CreateMinimumViable()
                .AddJob(Job.ShortRun.WithRuntime(new MonoRuntime(
                    "Mono x64", @"C:\Program Files\Mono\bin\mono.exe")))
                .AddJob(Job.ShortRun.WithRuntime(new MonoRuntime(
                    "Mono x86", @"C:\Program Files (x86)\Mono\bin\mono.exe"))));
        }

        [Benchmark]
        public void Foo()
        {
            // Benchmark body
        }
    }
}