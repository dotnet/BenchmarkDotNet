using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace BenchmarkDotNet.Samples.IL
{
    public struct BigValueType
    {
        public int _1, _2, _3, _4, _5, _6;
    }

    [LegacyJitX86Job, LegacyJitX64Job, RyuJitX64Job]
    public class IL_RefReturns
    {
        private BigValueType field;

        [Benchmark]
        public ref BigValueType ReturnsByRef() => ref Initialize(ref field);

        [Benchmark]
        public BigValueType ReturnsByValue() => Initialize(field);

        private ref BigValueType Initialize(ref BigValueType reference)
        {
            reference._1 = 1;
            reference._2 = 2;
            reference._3 = 3;
            reference._4 = 4;
            reference._5 = 5;
            reference._6 = 6;

            return ref reference;
        }

        private BigValueType Initialize(BigValueType value)
        {
            value._1 = 1;
            value._2 = 2;
            value._3 = 3;
            value._4 = 4;
            value._5 = 5;
            value._6 = 6;

            return value;
        }
    }
}
