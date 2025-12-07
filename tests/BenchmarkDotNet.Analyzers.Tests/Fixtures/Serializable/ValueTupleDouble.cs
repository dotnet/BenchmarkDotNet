using Xunit.Abstractions;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public class ValueTupleDouble<T1, T2> : IXunitSerializable
{
    public T1? Value1 { get; set; }

    public T2? Value2 { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Value1 = info.GetValue<T1>(nameof(Value1));
        Value2 = info.GetValue<T2>(nameof(Value2));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Value1), Value1);
        info.AddValue(nameof(Value2), Value2);
    }

    public static implicit operator ValueTupleDouble<T1, T2>((T1, T2) valueTupleDouble)
        => new() { Value1 = valueTupleDouble.Item1, Value2 = valueTupleDouble.Item2 };

    public override string ToString()
        => Value1 == null || Value2 == null ? "<empty>" : $"{Value1} · {Value2}";
}