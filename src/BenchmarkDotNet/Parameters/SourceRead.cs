using System.Reflection;

namespace BenchmarkDotNet.Parameters;

/// <summary>
/// One read of a [ParamsSource]/[ArgumentsSource] member: the member, and which of the values it yields.
/// </summary>
/// <remarks>
/// An arguments row is a single read that every parameter in it takes a value out of, so they share one instance
/// of this and differ only in <see cref="ParameterValue.FromSource.ElementIndex"/>. A toolchain can therefore emit
/// the read once and index into it, instead of recognising after the fact that several parameters would have
/// re-read the same member at the same index. A [ParamsSource] value is a read of its own: members range over
/// their values independently, so nothing is shared between them.
/// </remarks>
public sealed class SourceRead(MemberInfo source, int valueIndex)
{
    /// <summary>The source member (method or property) to enumerate.</summary>
    public MemberInfo Source { get; } = source;

    /// <summary>Index of the value within the sequence the source yields.</summary>
    public int ValueIndex { get; } = valueIndex;
}
