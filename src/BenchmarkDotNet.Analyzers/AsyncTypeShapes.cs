using Microsoft.CodeAnalysis;

namespace BenchmarkDotNet.Analyzers;

/// <summary>
/// Static-analysis counterparts to <c>ReflectionExtensions.IsAsyncEnumerable</c> and
/// <c>ReflectionExtensions.IsAwaitable</c> on the runtime side. Used by analyzers that need to mirror
/// the framework's `await foreach` / `await` binding shape rules at compile time.
/// </summary>
internal static class AsyncTypeShapes
{
    /// <summary>
    /// Returns true when <paramref name="type"/> would bind as an async enumerable under the C# compiler's
    /// `await foreach` rules: exact <c>IAsyncEnumerable&lt;T&gt;</c> short-circuit → public-instance
    /// <c>GetAsyncEnumerator</c> pattern with all-optional parameters whose return has public-instance
    /// <c>MoveNextAsync</c> (all-optional params) and a public <c>Current</c> property → interface fallback
    /// via <see cref="ITypeSymbol.AllInterfaces"/>.
    /// </summary>
    public static bool IsAsyncEnumerable(ITypeSymbol type, INamedTypeSymbol? asyncEnumerableInterfaceSymbol)
    {
        if (asyncEnumerableInterfaceSymbol != null
            && type is INamedTypeSymbol named
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, asyncEnumerableInterfaceSymbol))
        {
            return true;
        }

        if (TryFindPatternGetAsyncEnumerator(type) is { } enumeratorType
            && HasPatternMoveNextAsync(enumeratorType)
            && HasPublicInstanceProperty(enumeratorType, "Current"))
        {
            return true;
        }

        if (asyncEnumerableInterfaceSymbol != null)
        {
            foreach (var implemented in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, asyncEnumerableInterfaceSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true when <paramref name="type"/> exposes a public parameterless <c>GetAwaiter</c> method —
    /// the necessary precondition for the C# compiler's <c>await</c> binding. The analyzer doesn't drill
    /// into the awaiter's <c>IsCompleted</c>/<c>GetResult</c>/<c>OnCompleted</c> shape; the framework's
    /// runtime <c>IsAwaitable</c> check does that more thoroughly when needed.
    /// </summary>
    public static bool IsAwaitable(ITypeSymbol type)
    {
        foreach (var member in type.GetMembers("GetAwaiter"))
        {
            if (member is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false, Parameters.Length: 0 })
            {
                return true;
            }
        }
        return false;
    }

    private static ITypeSymbol? TryFindPatternGetAsyncEnumerator(ITypeSymbol type)
    {
        foreach (var member in type.GetMembers("GetAsyncEnumerator"))
        {
            if (member is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false } method
                && AllParametersOptional(method))
            {
                return method.ReturnType;
            }
        }
        return null;
    }

    private static bool HasPatternMoveNextAsync(ITypeSymbol enumeratorType)
    {
        foreach (var member in enumeratorType.GetMembers("MoveNextAsync"))
        {
            if (member is IMethodSymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false } method
                && AllParametersOptional(method))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasPublicInstanceProperty(ITypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
        {
            if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public, IsStatic: false })
            {
                return true;
            }
        }
        return false;
    }

    private static bool AllParametersOptional(IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (!parameter.IsOptional)
            {
                return false;
            }
        }
        return true;
    }
}
