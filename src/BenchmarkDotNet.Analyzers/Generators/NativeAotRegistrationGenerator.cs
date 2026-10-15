using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace BenchmarkDotNet.Analyzers.Generators;

/// <summary>
/// For a project published with Native AOT (<c>PublishAot</c> or <c>PublishTrimmed</c>), emits a module initializer that
/// lets BenchmarkDotNet run the project's benchmarks in-process without reflection-only code paths failing:
/// <list type="bullet">
/// <item>a <c>[DynamicDependency]</c> on every benchmark type (closed over its <c>[GenericTypeArguments]</c>), so the
/// trimmer keeps the members BenchmarkDotNet discovers by reflection, also through <c>BenchmarkSwitcher.FromAssembly</c>,
/// and a <c>NativeAotRegistration.RegisterBenchmarkType&lt;T&gt;()</c> call that creates it without reflection (a generic
/// type closed over a value type has no native code otherwise);</item>
/// <item>a <c>NativeAotRegistration.RegisterResultType&lt;T&gt;()</c> call for every result type whose benchmark action is a
/// generic type BenchmarkDotNet cannot instantiate at run time (a custom struct, a <c>ref</c> return, <c>ValueTask&lt;T&gt;</c>):
/// calling it with the concrete type is what makes the compiler generate that instantiation's native code.</item>
/// </list>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NativeAotRegistrationGenerator : IIncrementalGenerator
{
    private const string RegistrationTypeName = "BenchmarkDotNet.Running.NativeAotRegistration";
    private const string ModuleInitializerAttributeName = "System.Runtime.CompilerServices.ModuleInitializerAttribute";
    private const string GenericTypeArgumentsAttributeName = "BenchmarkDotNet.Attributes.GenericTypeArgumentsAttribute";
    private const string BenchmarkAttributeName = "BenchmarkDotNet.Attributes.BenchmarkAttribute";

    private static readonly HashSet<string> ActionMethodAttributeNames =
    [
        BenchmarkAttributeName,
        "BenchmarkDotNet.Attributes.GlobalSetupAttribute",
        "BenchmarkDotNet.Attributes.GlobalCleanupAttribute",
        "BenchmarkDotNet.Attributes.IterationSetupAttribute",
        "BenchmarkDotNet.Attributes.IterationCleanupAttribute",
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isEnabled = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
            IsTrue(options.GlobalOptions, "build_property.PublishAot") || IsTrue(options.GlobalOptions, "build_property.PublishTrimmed"));

        // The source text is the model: comparing it keeps the output cached while the benchmarks don't change.
        var source = context.CompilationProvider.Combine(isEnabled)
            .Select(static (pair, cancellationToken) => pair.Right ? Generate(pair.Left, cancellationToken) : null);

        context.RegisterSourceOutput(source, static (productionContext, text) =>
        {
            if (text != null)
                productionContext.AddSource("BenchmarkDotNet.NativeAotRegistration.g.cs", SourceText.From(text, Encoding.UTF8));
        });
    }

    private static bool IsTrue(Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions options, string key)
        => options.TryGetValue(key, out var value) && string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);

    internal static string? Generate(Compilation compilation, System.Threading.CancellationToken cancellationToken)
    {
        if (compilation.GetTypeByMetadataName(RegistrationTypeName) == null
            || compilation.GetTypeByMetadataName(ModuleInitializerAttributeName) == null)
        {
            return null;
        }

        var benchmarkTypes = new SortedSet<string>(System.StringComparer.Ordinal);
        var constructibleTypes = new SortedSet<string>(System.StringComparer.Ordinal);
        var resultTypes = new SortedSet<string>(System.StringComparer.Ordinal);

        foreach (var type in GetTypes(compilation.Assembly.GlobalNamespace))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic)
                continue;

            foreach (var closedType in Close(type))
            {
                if (!IsAccessible(compilation, closedType))
                    continue;

                var actionMethods = GetActionMethods(closedType).ToList();
                if (!actionMethods.Any(method => HasAttribute(method, BenchmarkAttributeName)))
                    continue;

                var name = closedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                benchmarkTypes.Add(name);
                if (closedType.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0 && compilation.IsSymbolAccessibleWithin(constructor, compilation.Assembly)))
                    constructibleTypes.Add(name);

                foreach (var method in actionMethods)
                {
                    var resultType = GetResultTypeToRegister(method);
                    if (resultType != null && IsAccessible(compilation, resultType))
                        resultTypes.Add(resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
        }

        if (benchmarkTypes.Count == 0)
            return null;

        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("// Generated by BenchmarkDotNet for a project published with Native AOT, see NativeAotRegistrationGenerator.");
        builder.AppendLine("#pragma warning disable CA2255 // The module initializer has to run before BenchmarkDotNet looks for the benchmarks.");
        builder.AppendLine();
        builder.AppendLine("namespace BenchmarkDotNet.Generated");
        builder.AppendLine("{");
        builder.AppendLine("    internal static class NativeAotBenchmarkRegistration");
        builder.AppendLine("    {");
        builder.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
        foreach (var benchmarkType in benchmarkTypes)
            builder.AppendLine($"        [global::System.Diagnostics.CodeAnalysis.DynamicDependency(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All, typeof({benchmarkType}))]");
        builder.AppendLine("        internal static void Register()");
        builder.AppendLine("        {");
        foreach (var benchmarkType in constructibleTypes)
            builder.AppendLine($"            global::{RegistrationTypeName}.RegisterBenchmarkType<{benchmarkType}>();");
        foreach (var resultType in resultTypes)
            builder.AppendLine($"            global::{RegistrationTypeName}.RegisterResultType<{resultType}>();");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers())
        {
            if (member is INamespaceSymbol ns)
            {
                foreach (var type in GetTypes(ns))
                    yield return type;
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in GetTypes(type))
                    yield return nested;
            }
        }
    }

    // An open generic benchmark type runs once per [GenericTypeArguments]; a type nested in a generic one never runs.
    private static IEnumerable<INamedTypeSymbol> Close(INamedTypeSymbol type)
    {
        if (type.ContainingType?.IsGenericType == true)
            yield break;

        if (type.TypeParameters.Length == 0)
        {
            yield return type;
            yield break;
        }

        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != GenericTypeArgumentsAttributeName)
                continue;

            var arguments = attribute.ConstructorArguments
                .SelectMany(argument => argument.Kind == TypedConstantKind.Array ? argument.Values : ImmutableArray.Create(argument))
                .Select(argument => argument.Value as ITypeSymbol)
                .ToArray();

            if (arguments.Length == type.TypeParameters.Length && arguments.All(argument => argument != null))
                yield return type.Construct(arguments!);
        }
    }

    // BenchmarkDotNet finds the methods on the type and its base types.
    private static IEnumerable<IMethodSymbol> GetActionMethods(INamedTypeSymbol type)
    {
        for (var current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind == MethodKind.Ordinary && method.GetAttributes().Any(attribute => ActionMethodAttributeNames.Contains(attribute.AttributeClass?.ToDisplayString() ?? "")))
                    yield return method;
            }
        }
    }

    private static bool HasAttribute(IMethodSymbol method, string attributeName)
        => method.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == attributeName);

    // The types BenchmarkDotNet cannot handle without a registered instantiation (see AotBenchmarkActions): what a ref
    // return refers to, the result of a ValueTask<T>, the items of an IAsyncEnumerable<T> of a value type, and any other
    // value type. Reference types and Task<T> are handled without one.
    private static ITypeSymbol? GetResultTypeToRegister(IMethodSymbol method)
    {
        var type = method.ReturnType;

        var candidate = method.ReturnsByRef || method.ReturnsByRefReadonly ? type
            : type.SpecialType == SpecialType.System_Void ? null
            : type is INamedTypeSymbol { IsGenericType: true } named && IsGeneric(named, "System.Threading.Tasks.ValueTask`1") ? named.TypeArguments[0]
            : type is INamedTypeSymbol { IsGenericType: true } enumerable && IsGeneric(enumerable, "System.Collections.Generic.IAsyncEnumerable`1")
                ? (enumerable.TypeArguments[0].IsValueType ? enumerable.TypeArguments[0] : null)
            : type.IsValueType && !IsNonGenericTask(type) ? type
            : null;

        return candidate is { IsRefLikeType: false } && candidate.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer or TypeKind.TypeParameter or TypeKind.Dynamic or TypeKind.Error)
            ? candidate
            : null;
    }

    private static bool IsGeneric(INamedTypeSymbol type, string metadataName)
        => type.OriginalDefinition.ContainingNamespace?.ToDisplayString() + "." + type.OriginalDefinition.MetadataName == metadataName;

    private static bool IsNonGenericTask(ITypeSymbol type)
        => type is INamedTypeSymbol { IsGenericType: false } named && named.ToDisplayString() == "System.Threading.Tasks.ValueTask";

    // The generated code is part of the assembly, so internal types can be used, but private or protected ones cannot.
    private static bool IsAccessible(Compilation compilation, ITypeSymbol type)
        => type switch
        {
            IArrayTypeSymbol array => IsAccessible(compilation, array.ElementType),
            INamedTypeSymbol named => compilation.IsSymbolAccessibleWithin(named.OriginalDefinition, compilation.Assembly)
                && named.TypeArguments.All(argument => IsAccessible(compilation, argument)),
            _ => compilation.IsSymbolAccessibleWithin(type, compilation.Assembly),
        };
}
