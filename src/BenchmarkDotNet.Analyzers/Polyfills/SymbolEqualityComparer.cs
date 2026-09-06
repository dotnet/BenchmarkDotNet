#if !CODE_ANALYSIS_3_8
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis;

// SymbolEqualityComparer appears in Roslyn 3.3, so the 2.8 and 3.0 bands compare symbols through ISymbol.Equals,
// as callers did before it existed. Declared in Roslyn's own namespace, so the call sites read the same on
// every band.
internal sealed class SymbolEqualityComparer : IEqualityComparer<ISymbol>
{
    internal static readonly SymbolEqualityComparer Default = new();

    private SymbolEqualityComparer() { }

    public bool Equals(ISymbol? x, ISymbol? y) => x is null ? y is null : x.Equals(y);

    public int GetHashCode(ISymbol obj) => obj.GetHashCode();
}
#endif
