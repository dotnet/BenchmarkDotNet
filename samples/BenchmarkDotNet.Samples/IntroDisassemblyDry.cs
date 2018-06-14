using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;

namespace BenchmarkDotNet.Samples
{
    [Config(typeof(JustDisassembly))]
    public class IntroDisassemblyDry
    {
        public class JustDisassembly : ManualConfig
        {
            public JustDisassembly()
            {
                Add(Job.Dry.With(Jit.RyuJit).With(Platform.X64).With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp20));
                Add(Job.Dry.With(Jit.RyuJit).With(Platform.X64).With(Runtime.Core).With(CsProjCoreToolchain.NetCoreApp21));

                Add(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(printAsm: true, printPrologAndEpilog: true, recursiveDepth: 3)));
            }
        }

        [Benchmark]
        public void Foo()
        {
            
        }
    }
}