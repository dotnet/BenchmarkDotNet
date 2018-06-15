using BenchmarkDotNet.Attributes;

namespace BenchmarkDotNet.IntegrationTests.ManualRunning.Smoke
{
    [MedianColumn, Q3Column, MaxColumn]
    [LegacyJitX64Job, RyuJitX64Job, MonoJob]
    [KeepBenchmarkFiles]
    public class SmokeEmpty
    {
        [Benchmark] public void Void1() {}
        [Benchmark] public void Void2() {}
        [Benchmark] public void Void3() {}
        [Benchmark] public void Void4() {}

        [Benchmark] public byte Byte1() => 0;
        [Benchmark] public byte Byte2() => 0;
        [Benchmark] public byte Byte3() => 0;
        [Benchmark] public byte Byte4() => 0;

        [Benchmark] public sbyte Sbyte1() => 0;
        [Benchmark] public sbyte Sbyte2() => 0;
        [Benchmark] public sbyte Sbyte3() => 0;
        [Benchmark] public sbyte Sbyte4() => 0;

        [Benchmark] public short Short1() => 0;
        [Benchmark] public short Short2() => 0;
        [Benchmark] public short Short3() => 0;
        [Benchmark] public short Short4() => 0;

        [Benchmark] public ushort Ushort1() => 0;
        [Benchmark] public ushort Ushort2() => 0;
        [Benchmark] public ushort Ushort3() => 0;
        [Benchmark] public ushort Ushort4() => 0;

        [Benchmark] public int Int1() => 0;
        [Benchmark] public int Int2() => 0;
        [Benchmark] public int Int3() => 0;
        [Benchmark] public int Int4() => 0;

        [Benchmark] public uint Uint1() => 0u;
        [Benchmark] public uint Uint2() => 0u;
        [Benchmark] public uint Uint3() => 0u;
        [Benchmark] public uint Uint4() => 0u;

        [Benchmark] public bool Bool1() => false;
        [Benchmark] public bool Bool2() => false;
        [Benchmark] public bool Bool3() => false;
        [Benchmark] public bool Bool4() => false;

        [Benchmark] public char Char1() => 'a';
        [Benchmark] public char Char2() => 'a';
        [Benchmark] public char Char3() => 'a';
        [Benchmark] public char Char4() => 'a';

        [Benchmark] public float Float1() => 0f;
        [Benchmark] public float Float2() => 0f;
        [Benchmark] public float Float3() => 0f;
        [Benchmark] public float Float4() => 0f;

        [Benchmark] public double Double1() => 0d;
        [Benchmark] public double Double2() => 0d;
        [Benchmark] public double Double3() => 0d;
        [Benchmark] public double Double4() => 0d;

        [Benchmark] public long Long1() => 0L;
        [Benchmark] public long Long2() => 0L;
        [Benchmark] public long Long3() => 0L;
        [Benchmark] public long Long4() => 0L;

        [Benchmark] public ulong Ulong1() => 0uL;
        [Benchmark] public ulong Ulong2() => 0uL;
        [Benchmark] public ulong Ulong3() => 0uL;
        [Benchmark] public ulong Ulong4() => 0uL;

        [Benchmark] public string String1() => "";
        [Benchmark] public string String2() => "";
        [Benchmark] public string String3() => "";
        [Benchmark] public string String4() => "";

        [Benchmark] public object Object1() => null;
        [Benchmark] public object Object2() => null;
        [Benchmark] public object Object3() => null;
        [Benchmark] public object Object4() => null;
    }
}