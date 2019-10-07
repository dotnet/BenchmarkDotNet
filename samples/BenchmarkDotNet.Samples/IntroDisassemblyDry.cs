using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(JustDisassembly))]
    public class IntroDisassemblyDry
    {
        public class JustDisassembly : ManualConfig
        {
            public JustDisassembly()
            {
                AddJob(Job.Dry.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(CoreRuntime.Core20));
                AddJob(Job.Dry.WithJit(Jit.RyuJit).WithPlatform(Platform.X64).WithRuntime(CoreRuntime.Core21));

                AddDiagnoser(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, printPrologAndEpilog: true, recursiveDepth: 3)));
            }
        }

        [Benchmark]
        public void Foo()
        {

        }
    }
}