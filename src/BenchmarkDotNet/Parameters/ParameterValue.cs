namespace BenchmarkDotNet.Parameters;

/// <summary>
/// One value a benchmark parameter can take, and a language-neutral description for a toolchain to re-create it in generated code.
/// </summary>
public abstract class ParameterValue
{
    /// <summary>The value of the parameter.</summary>
    public object? Value { get; }

    private ParameterValue(object? value) => Value = value;

    /// <summary>A value the toolchain can embed directly, e.g. a primitive, string, enum, array, or <see cref="System.Type"/>.</summary>
    public sealed class Constant(object? value, Type type) : ParameterValue(value)
    {
        /// <summary>The declared type of the parameter.</summary>
        /// <remarks>
        /// Needed because the value alone can be ambiguous - an enum declared in F# is erased to its underlying type in attribute metadata (dotnet/fsharp#995).
        /// </remarks>
        public Type Type { get; } = type;
    }

    /// <summary>
    /// A value the toolchain cannot embed, so the generated code re-obtains it by enumerating the [ParamsSource]/[ArgumentsSource] member it originally came from.
    /// </summary>
    public sealed class FromSource(object? value, SourceRead read, int? elementIndex, Type targetType) : ParameterValue(value)
    {
        /// <summary>The read this value comes out of, shared by every parameter bound from the same one.</summary>
        public SourceRead Read { get; } = read;

        /// <summary>
        /// The index of the value within the yielded element, if the element is an args-list array; <see langword="null"/> otherwise.
        /// </summary>
        public int? ElementIndex { get; } = elementIndex;

        /// <summary>The type the obtained value is used as.</summary>
        public Type TargetType { get; } = targetType;
    }
}
