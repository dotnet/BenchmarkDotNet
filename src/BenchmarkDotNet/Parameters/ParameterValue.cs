using BenchmarkDotNet.Extensions;

namespace BenchmarkDotNet.Parameters;

/// <summary>
/// One value a benchmark parameter can take, and a language-neutral description for a toolchain to re-create it in generated code.
/// </summary>
public abstract class ParameterValue
{
    /// <summary>The value of the parameter.</summary>
    public object? Value { get; }

    // #774
    /// <summary>
    /// The type the generated code writes this value as, and holds it in a field as, which is not always what the
    /// parameter is declared as - <see cref="ParameterDefinition.ParameterType"/> says that. A by-ref-like
    /// parameter is where the two part: nothing can hold one of those in a field, so the value is held as
    /// the type its conversion operator takes and converted where the argument is loaded.
    /// </summary>
    /// <remarks>
    /// Carried rather than read back off the value, because the value alone can be ambiguous - an enum declared in F# is erased to its underlying type in attribute metadata (dotnet/fsharp#995).
    /// </remarks>
    public Type SourceType { get; }

    private ParameterValue(object? value, Type sourceType)
    {
        Value = value;
        SourceType = sourceType;
    }

    /// <summary>A value the toolchain can embed directly, e.g. a primitive, string, enum, array, or <see cref="System.Type"/>.</summary>
    public sealed class Constant(object? value, Type sourceType) : ParameterValue(value, sourceType);

    /// <summary>
    /// A value the toolchain cannot embed, so the generated code re-obtains it by enumerating the [ParamsSource]/[ArgumentsSource] member it originally came from.
    /// </summary>
    public sealed class FromSource(object? value, SourceRead read, int? elementIndex, Type sourceType) : ParameterValue(value, sourceType)
    {
        /// <summary>The read this value comes out of, shared by every parameter bound from the same one.</summary>
        public SourceRead Read { get; } = read;

        /// <summary>
        /// The index of the value within the yielded element, if the element is an args-list array; <see langword="null"/> otherwise.
        /// </summary>
        public int? ElementIndex { get; } = elementIndex;
    }
}
