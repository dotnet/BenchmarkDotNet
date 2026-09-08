#if !CODE_ANALYSIS_3_0
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis;

// ITypeSymbol.IsRefLikeType only appears in Roslyn 3.0 (dotnet/roslyn#30426), though the compiler has tracked
// ref-likeness since C# 7.2 - as the internal TypeSymbol.IsByRefLikeType this reads. Reflection rather than the
// metadata attribute because that route is deliberately closed: PENamedTypeSymbol.GetAttributes filters
// IsByRefLikeAttribute out exactly when the type is ref-like, so a referenced Span<T> reports no such attribute.
// The property is declared on the base and overridden per symbol kind, so one lookup serves every kind.
//
// Both guards below answer false rather than throwing, because an exception escaping an analyzer is reported as
// AD0001 and disables it. The lookup is guarded because a failure in a static initializer poisons the type for
// every later call; the instance check is guarded because GetValue is the one call here that can throw, and does
// so only for an ITypeSymbol that is not a Roslyn C# symbol - which the language filter on every analyzer here
// makes unreachable, RS1009 forbids implementing, and a runtime mock could still produce.
internal static class RefLikeTypePolyfill
{
    private static readonly PropertyInfo? IsByRefLikeType = ResolveIsByRefLikeType();

    internal static bool IsRefLikeType(ITypeSymbol type)
        => IsByRefLikeType is { } property
        && property.DeclaringType!.IsInstanceOfType(type)
        && property.GetValue(type) is true;

    private static PropertyInfo? ResolveIsByRefLikeType()
    {
        try
        {
            return typeof(CSharpCompilation).Assembly
                .GetType("Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol")
                ?.GetProperty("IsByRefLikeType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        catch
        {
            return null;
        }
    }
}
#endif
