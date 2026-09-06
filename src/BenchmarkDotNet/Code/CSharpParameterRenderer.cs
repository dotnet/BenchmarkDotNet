using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using System.Reflection;

namespace BenchmarkDotNet.Code;

/// <summary>
/// Renders <see cref="ParameterValue"/> descriptions as C# expressions for the generated runnable.
/// All generated syntax lives here, so the parameters themselves stay language-neutral.
/// </summary>
internal sealed class CSharpParameterRenderer
{
    // The CancellationToken local declared at the top of the generated Run method.
    private const string CancellationTokenLocalName = "cancellationToken";

    // One entry per SourceRead reached through an instance member, in assignment order. The member is invoked in
    // the constructor - the only place `base` is reachable - and the sequence passed out to Run, which extracts
    // from it. Keyed by the read rather than by the parameter, so an arguments row invokes the member once; two
    // [ParamsSource] members naming the same one still get a local each, because members range over their values
    // independently and each read is its own object. Names are positional, so two sources sharing a simple name
    // cannot collide.
    private readonly IReadOnlyList<(SourceRead Read, string Local)> instanceReads;

    // One entry per SourceRead whose values are assigned by statement rather than by the object initializer -
    // arguments and static [ParamsSource] members. A statement block can hold a local, so the read is extracted
    // once into one and every parameter indexes into that. The initializer cannot, so a read used only there stays
    // an inline expression; nothing is shared between members anyway.
    private readonly IReadOnlyList<(SourceRead Read, string Local)> statementReads;

    private CSharpParameterRenderer(
        IReadOnlyList<(SourceRead, string)> instanceReads,
        IReadOnlyList<(SourceRead, string)> statementReads)
    {
        this.instanceReads = instanceReads;
        this.statementReads = statementReads;
    }

    public static CSharpParameterRenderer Create(BenchmarkCase benchmarkCase)
    {
        var instanceReads = new List<(SourceRead, string)>();
        var statementReads = new List<(SourceRead, string)>();

        foreach (var parameter in benchmarkCase.Parameters.Items)
        {
            if (parameter.ParameterValue is not ParameterValue.FromSource fromSource)
                continue;

            if (!IsStatic(fromSource.Read.Source))
                Reserve(instanceReads, fromSource.Read, "source");

            // Everything the object initializer does not assign is assigned by a statement, which can hold a local.
            if (parameter.IsArgument || parameter.IsStatic)
                Reserve(statementReads, fromSource.Read, "read");
        }

        return new CSharpParameterRenderer(instanceReads, statementReads);

        static void Reserve(List<(SourceRead, string)> reads, SourceRead read, string prefix)
        {
            foreach (var reserved in reads)
            {
                if (ReferenceEquals(reserved.Item1, read))
                    return;
            }

            reads.Add((read, prefix + reads.Count));
        }
    }

    /// <summary>`out IEnumerable&lt;T&gt; source0, ...` - the ctor's parameter list, and the `new` call's argument list.</summary>
    /// <remarks>`out T x` parses both as a parameter declaration and as an out-variable declaration expression,
    /// so the same text serves the declaration and the call site.</remarks>
    public string RenderSourceOutParameters()
        => string.Join(", ", instanceReads.Select(read =>
            $"out {ReturnType(read.Read.Source).GetCorrectCSharpTypeName()} {read.Local}"));

    /// <summary>`source0 = base.Values();` - the ctor body statements that read the instance sources.</summary>
    public string RenderSourceCaptures()
        => string.Join(
            Environment.NewLine,
            instanceReads.Select(read => $"            {read.Local} = base.{read.Read.Source.Name}{InvocationPostfix(read.Read.Source)};"));

    /// <summary>`Element read0 = ...;` - one extraction per read, ahead of the statements that index into it.</summary>
    public IEnumerable<string> RenderStatementReads()
        => statementReads.Select(read => $"{ElementTypeName(read.Read)} {read.Local} = {Extraction(read.Read)};");

    public string Render(ParameterValue value)
    {
        switch (value)
        {
            case ParameterValue.Constant constant:
                return SourceCodeHelper.ToSourceCode(constant.Value, constant.Type);

            case ParameterValue.FromSource fromSource:
            {
                // The value can't be embedded, so the child process re-obtains it by enumerating the source.
                // GetParameterAsync returns the source's element type, so an element index binds directly.
                string cast = $"({fromSource.TargetType.GetCorrectCSharpTypeName()})";
                string elementIndex = fromSource.ElementIndex is { } index ? $"[{index}]" : string.Empty;

                string source = StatementLocal(fromSource.Read) is { } local ? local : $"({Extraction(fromSource.Read)})";

                return $"{cast}{source}{elementIndex}";
            }

            default:
                throw new NotSupportedException($"{value.GetType().Name} is not a supported {nameof(ParameterValue)}.");
        }
    }

    // The generated code declares its locals with explicit, fully qualified types, as the template does.
    private static string ElementTypeName(SourceRead read)
    {
        TryGetElementTypeName(read, out string name);

        return name;
    }

    // False where the source does not name an element type. The extraction then has no type argument to bind
    // against and does not compile, which is reported before any of this is emitted - `var` only keeps the
    // declaration from adding a second error on top of that one.
    private static bool TryGetElementTypeName(SourceRead read, out string name)
    {
        if (read.Source.GetSourceReturnType().TryGetSourceElementType(out var elementType))
        {
            name = elementType.GetCorrectCSharpTypeName();
            return true;
        }

        name = "var";
        return false;
    }

    // Fully qualified (#778, #1007, #2821).
    private string Extraction(SourceRead read)
    {
        string typeArgument = TryGetElementTypeName(read, out string elementTypeName)
            ? $"<{elementTypeName}>"
            : string.Empty;

        return $"await global::BenchmarkDotNet.Helpers.AwaitHelper.ConfigureAwait(global::BenchmarkDotNet.Parameters.ParameterExtractor.GetParameterAsync{typeArgument}({SourceExpression(read)}, {read.ValueIndex}, {CancellationTokenLocalName}))";
    }

    private string? StatementLocal(SourceRead read)
    {
        foreach (var reserved in statementReads)
        {
            if (ReferenceEquals(reserved.Read, read))
                return reserved.Local;
        }

        return null;
    }

    // A static source is called where the value is assigned; an instance source is read into a local in the
    // constructor, because `base` is not reachable from Run's object initializer.
    private string SourceExpression(SourceRead read)
    {
        if (IsStatic(read.Source))
            return $"{read.Source.DeclaringType!.GetCorrectCSharpTypeName()}.{read.Source.Name}{InvocationPostfix(read.Source)}";

        foreach (var reserved in instanceReads)
        {
            if (ReferenceEquals(reserved.Read, read))
                return reserved.Local;
        }

        throw new InvalidOperationException($"No local was reserved for the value {read.Source.Name} provides.");
    }

    private static string InvocationPostfix(MemberInfo source) => source is PropertyInfo ? string.Empty : "()";

    private static bool IsStatic(MemberInfo source)
        => source is PropertyInfo property ? property.GetMethod!.IsStatic : ((MethodInfo) source).IsStatic;

    private static Type ReturnType(MemberInfo source)
        => source is PropertyInfo property ? property.GetMethod!.ReturnType : ((MethodInfo) source).ReturnType;
}
