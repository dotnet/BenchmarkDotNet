using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace BenchmarkDotNet.Samples
{
    [DisassemblyDiagnoser(maxDepth: 3)]
    [DryJob]
    public class IntroDisassemblyDry
    {
        [Benchmark]
        public void Foo()
        {

        }
    }
}