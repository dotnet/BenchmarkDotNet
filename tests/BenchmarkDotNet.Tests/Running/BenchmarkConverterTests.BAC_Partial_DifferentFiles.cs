using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.Tests.Running
{
    public partial class BenchmarkConverterTests
    {
        public partial class BAC_Partial_DifferentFiles
        {
            [Benchmark] public void B() { }
        }
    }
}
