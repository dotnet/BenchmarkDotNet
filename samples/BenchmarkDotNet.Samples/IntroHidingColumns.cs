using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Samples
{
    [MemoryDiagnoser] // adds Gen0, Gen1, Gen2 and Allocated Bytes columns
    [HideColumns(Column.Gen0, Column.Gen1, Column.Gen2)] // dont display GenX columns
    public class IntroHidingColumns
    {
        [Benchmark]
        public byte[] AllocateArray() => new byte[100_000];
    }
}
