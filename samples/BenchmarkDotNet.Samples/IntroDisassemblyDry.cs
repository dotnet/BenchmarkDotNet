using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Samples
{
    [DisassemblyDiagnoser(maxDepth: 3)]
    [DryJob]
    [UseLocalJobOnly]
    public class IntroDisassemblyDry
    {
        [Benchmark]
        public void Foo()
        {

        }
    }
}