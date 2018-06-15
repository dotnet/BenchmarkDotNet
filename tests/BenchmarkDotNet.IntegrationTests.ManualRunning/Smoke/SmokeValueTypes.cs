using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Environments;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning.Smoke
{
    // ReSharper disable InconsistentNaming

    [RyuJitX64Job, LegacyJitX64Job, LegacyJitX86Job]
    [MemoryDiagnoser]
    public class SmokeValueTypes
    {
        [Benchmark] public Jit ReturnEnum() => Jit.RyuJit;

        [Benchmark] public DateTime ReturnDateTime() => new DateTime();

        [Benchmark] public DateTime? ReturnNullableDateTime() => new DateTime();
        [Benchmark] public int? ReturnNullableInt() => 0;

        public struct StructWithReferencesOnly { public object _ref; }
        [Benchmark] public StructWithReferencesOnly ReturnStructWithReferencesOnly() => new StructWithReferencesOnly();

        public struct EmptyStruct { }
        [Benchmark] public EmptyStruct ReturnEmptyStruct() => new EmptyStruct();

        [Benchmark] public ValueTuple<int> ReturnGenericStructOfValueType() => new ValueTuple<int>(0);
        [Benchmark] public ValueTuple<object> ReturnGenericStructOfReferenceType() => new ValueTuple<object>(null);

        [Benchmark] public ValueTask<int> ReturnValueTaskOfValueType() => new ValueTask<int>(0);
        [Benchmark] public ValueTask<object> ReturnValueTaskOfReferenceType() => new ValueTask<object>(result: null);

        [Benchmark] public byte ReturnByte() => 0;
        public struct Byte1 { public byte _1; }
        [Benchmark] public Byte1 ReturnByte1() => new Byte1();
        public struct Byte2 { public byte _1, _2; }
        [Benchmark] public Byte2 ReturnByte2() => new Byte2();
        public struct Byte3 { public byte _1, _2, _3; }
        [Benchmark] public Byte3 ReturnByte3() => new Byte3();
        public struct Byte4 { public byte _1, _2, _3, _4; }
        [Benchmark] public Byte4 ReturnByte4() => new Byte4();

        [Benchmark] public short ReturnShort() => 0;
        public struct Short1 { public short _1; }
        [Benchmark] public Short1 ReturnShort1() => new Short1();
        public struct Short2 { public short _1, _2; }
        [Benchmark] public Short2 ReturnShort2() => new Short2();
        public struct Short3 { public short _1, _2, _3; }
        [Benchmark] public Short3 ReturnShort3() => new Short3();
        public struct Short4 { public short _1, _2, _3, _4; }
        [Benchmark] public Short4 ReturnShort4() => new Short4();

        [Benchmark] public int ReturnInt() => 0;
        public struct Int1 { public int _1; }
        [Benchmark] public Int1 ReturnInt1() => new Int1();
        public struct Int2 { public int _1, _2; }
        [Benchmark] public Int2 ReturnInt2() => new Int2();
        public struct Int3 { public int _1, _2, _3; }
        [Benchmark] public Int3 ReturnInt3() => new Int3();
        public struct Int4 { public int _1, _2, _3, _4; }
        [Benchmark] public Int4 ReturnInt4() => new Int4();

        [Benchmark] public long ReturnLong() => 0;
        public struct Long1 { public long _1; }
        [Benchmark] public Long1 ReturnLong1() => new Long1();
        public struct Long2 { public long _1, _2; }
        [Benchmark] public Long2 ReturnLong2() => new Long2();
        public struct Long3 { public long _1, _2, _3; }
        [Benchmark] public Long3 ReturnLong3() => new Long3();
        public struct Long4 { public long _1, _2, _3, _4; }
        [Benchmark] public Long4 ReturnLong4() => new Long4();
    }
    // ReSharper restore InconsistentNaming
}