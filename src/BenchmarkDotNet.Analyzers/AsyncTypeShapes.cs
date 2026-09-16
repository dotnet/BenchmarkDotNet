using System.Collections.Generic;
using System.Linq;
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

        if (TryFindPatternGetAsyncEnumerator(type) is { } enumeratorType)
        {
            // Roslyn commits to a found pattern `GetAsyncEnumerator` — if its return type doesn't
            // satisfy the await-foreach enumerator shape it reports an error instead of falling back
            // to `IAsyncEnumerable<T>`, even when the source also implements the interface. We mirror
            // that here so the analyzer's view of binding matches what `await foreach` would actually
            // accept.
            return HasPatternMoveNextAsync(enumeratorType)
                && HasPublicInstanceProperty(enumeratorType, "Current");
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
    /// Whether a [ParamsSource]/[ArgumentsSource] member's return type offers no usable source shape at all -
    /// neither <c>IEnumerable&lt;T&gt;</c> nor <c>IAsyncEnumerable&lt;T&gt;</c>. The non-generic
    /// <c>System.Collections.IEnumerable</c> does not qualify on its own: the generated extraction call infers its
    /// element type from the source, and a type with no generic instantiation gives inference nothing to bind to.
    /// The await-foreach pattern without the <c>IAsyncEnumerable&lt;T&gt;</c> interface is not supported either.
    /// </summary>
    public static bool IsSupportedSourceReturnType(Compilation compilation, ITypeSymbol returnType)
        => CountSourceShapes(compilation, returnType) >= 1;

    /// <summary>
    /// Whether a source's return type offers more than one candidate shape, which BenchmarkDotNet cannot read
    /// unambiguously - both an enumerable and an async enumerable, or several instantiations of either one
    /// (<c>IEnumerable&lt;int&gt;</c> plus <c>IEnumerable&lt;string&gt;</c>, say).
    /// </summary>
    public static bool IsAmbiguouslyEnumerable(Compilation compilation, ITypeSymbol returnType)
        => CountSourceShapes(compilation, returnType) > 1;

    /// <summary>
    /// The element type a source declares, when its shape is unambiguous - the T of the single
    /// <c>IEnumerable&lt;T&gt;</c> or <c>IAsyncEnumerable&lt;T&gt;</c> it offers. That is what the generated
    /// extraction call returns, and so what any generated index is applied to.
    /// </summary>
    public static bool TryGetSourceElementType(Compilation compilation, ITypeSymbol returnType, out ITypeSymbol? elementType)
    {
        elementType = null;
        if (CountSourceShapes(compilation, returnType) != 1)
        {
            return false;
        }

        var enumerable = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
        var asyncEnumerable = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");

        foreach (var candidate in new[] { returnType }.Concat(returnType.AllInterfaces))
        {
            if (candidate is INamedTypeSymbol { IsGenericType: true } named
                && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, enumerable)
                    || SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, asyncEnumerable)))
            {
                elementType = named.TypeArguments[0];
                return true;
            }
        }
        return false;
    }

    // Type inference needs a *unique* candidate interface, so anything other than exactly one instantiation across
    // both shapes fails to compile in the generated code (CS0411) - even when one element type converts to the
    // other, as with IEnumerable<string> plus IEnumerable<object>. Counting keeps the two rules disjoint: none
    // reports "unsupported shape", several reports "ambiguous shape".
    private static int CountSourceShapes(Compilation compilation, ITypeSymbol returnType)
        => CountInstantiations(returnType, compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))
         + CountInstantiations(returnType, compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"));

    /// <summary>
    /// Counts the distinct closed instantiations of <paramref name="interfaceDefinition"/> that
    /// <paramref name="type"/> either is or implements.
    /// </summary>
    private static int CountInstantiations(ITypeSymbol type, INamedTypeSymbol? interfaceDefinition)
    {
        if (interfaceDefinition == null)
        {
            return 0;
        }

        var found = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, interfaceDefinition))
        {
            found.Add(named);
        }
        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, interfaceDefinition))
            {
                found.Add(implemented);
            }
        }
        return found.Count;
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
