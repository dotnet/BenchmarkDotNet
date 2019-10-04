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
                AddJob(Job.Dry.With(Jit.RyuJit).With(Platform.X64).With(CoreRuntime.Core20));
                AddJob(Job.Dry.With(Jit.RyuJit).With(Platform.X64).With(CoreRuntime.Core21));

                AddDiagnoser(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, printPrologAndEpilog: true, recursiveDepth: 3)));
            }
        }

        [Benchmark]
        public void Foo()
        {

        }
    }
}