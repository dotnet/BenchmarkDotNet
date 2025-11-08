using Xunit.Abstractions;

namespace BenchmarkDotNet.Analyzers.Tests.Fixtures;

public class ValueTupleTriple<T1, T2, T3> : IXunitSerializable
{
    public T1? Value1 { get; set; }

    public T2? Value2 { get; set; }

    public T3? Value3 { get; set; }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Value1 = info.GetValue<T1>(nameof(Value1));
        Value2 = info.GetValue<T2>(nameof(Value2));
        Value3 = info.GetValue<T3>(nameof(Value3));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Value1), Value1);
        info.AddValue(nameof(Value2), Value2);
        info.AddValue(nameof(Value3), Value3);
    }

    public static implicit operator ValueTupleTriple<T1, T2, T3>((T1, T2, T3) valueTupleTriple)
        => new() { Value1 = valueTupleTriple.Item1, Value2 = valueTupleTriple.Item2, Value3 = valueTupleTriple.Item3 };

    public override string ToString()
        => Value1 == null || Value2 == null || Value3 == null ? "<empty>" : $"{Value1} · {Value2} · {Value3}";
}