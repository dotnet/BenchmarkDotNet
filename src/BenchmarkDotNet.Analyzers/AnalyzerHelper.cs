using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;

namespace BenchmarkDotNet.Analyzers;

internal static class AnalyzerHelper
{
    internal const string InterceptorsNamespaces = "InterceptorsNamespaces";

    public static LocalizableResourceString GetResourceString(string name)
        => new(name, BenchmarkDotNetAnalyzerResources.ResourceManager, typeof(BenchmarkDotNetAnalyzerResources));

    // Shared by the [ParamsSource] and [ArgumentsSource] analyzers: the runtime only invokes a source method whose
    // parameters are all optional, so a method with a required parameter isn't recognized as a source.
    public static readonly DiagnosticDescriptor SourceMethodMustNotHaveRequiredParametersRule = new(
        DiagnosticIds.General_Source_MethodMustNotHaveRequiredParameters,
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotHaveRequiredParameters_Title)),
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotHaveRequiredParameters_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotHaveRequiredParameters_Description)));

    // Shared by the [ParamsSource] and [ArgumentsSource] analyzers: the runtime invokes a source method directly,
    // and nothing supplies a generic one's type arguments.
    public static readonly DiagnosticDescriptor SourceMethodMustNotBeGenericRule = new(
        DiagnosticIds.General_Source_MethodMustNotBeGeneric,
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotBeGeneric_Title)),
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotBeGeneric_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_MethodMustNotBeGeneric_Description)));

    // Shared by both source analyzers: discovery reads a source's values into an object[], and a ref struct cannot
    // be boxed. Expressible since .NET 10 gave IEnumerable<T> an allows-ref-struct type parameter.
    public static readonly DiagnosticDescriptor SourceElementMustNotBeByRefLikeRule = new(
        DiagnosticIds.General_Source_ElementMustNotBeByRefLike,
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMustNotBeByRefLike_Title)),
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMustNotBeByRefLike_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMustNotBeByRefLike_Description)));

    // The constraint case, which the compiler cannot decide: a type argument that is not by-ref-like reads
    // normally, so this warns where the rule above - a ref struct the compiler can see - is an error.
    public static readonly DiagnosticDescriptor SourceElementMayBeByRefLikeRule = new(
        DiagnosticIds.General_Source_ElementMayBeByRefLike,
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMayBeByRefLike_Title)),
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMayBeByRefLike_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_ElementMayBeByRefLike_Description)));

    // Shared by both source analyzers: a source that is both shapes cannot be read unambiguously - discovery
    // takes the synchronous path while the generated code cannot pick a GetParameterAsync overload.
    public static readonly DiagnosticDescriptor SourceMustNotBeAmbiguouslyEnumerableRule = new(
        DiagnosticIds.General_Source_AmbiguousEnumerableShape,
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_AmbiguousEnumerableShape_Title)),
        GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_AmbiguousEnumerableShape_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Source_AmbiguousEnumerableShape_Description)));

    // Mirrors GetValidValuesForParamsSourceAsync, which passes over generic methods. True only when a public
    // generic method of that name exists and nothing else can serve it - the runtime would have nothing to invoke.
    public static bool SourceResolvesOnlyToGenericMethod(ITypeSymbol type, string name)
    {
        bool anyGenericMethod = false;
        bool anyInvocableMethod = false;
        bool anyReadableProperty = false;

        for (ITypeSymbol? current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public)
                {
                    if (method.IsGenericMethod)
                    {
                        anyGenericMethod = true;
                    }
                    else if (method.Parameters.All(parameter => parameter.IsOptional))
                    {
                        anyInvocableMethod = true;
                    }
                }
                else if (member is IPropertySymbol property && property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                {
                    anyReadableProperty = true;
                }
            }
        }

        return anyGenericMethod && !anyInvocableMethod && !anyReadableProperty;
    }

    // Mirrors GetValidValuesForParamsSourceAsync: a public all-optional method, else a property with a public
    // getter. True only when every candidate of that name has required parameters, bases included.
    public static bool SourceResolvesOnlyToRequiredParameterMethod(ITypeSymbol type, string name)
    {
        bool anyPublicMethod = false;
        bool anyAllOptionalMethod = false;
        bool anyReadableProperty = false;

        for (ITypeSymbol? current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary && method.DeclaredAccessibility == Accessibility.Public)
                {
                    anyPublicMethod = true;
                    if (method.Parameters.All(parameter => parameter.IsOptional))
                    {
                        anyAllOptionalMethod = true;
                    }
                }
                else if (member is IPropertySymbol property && property.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                {
                    anyReadableProperty = true;
                }
            }
        }

        return anyPublicMethod && !anyAllOptionalMethod && !anyReadableProperty;
    }

    public static INamedTypeSymbol? GetBenchmarkAttributeTypeSymbol(Compilation compilation)
        => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

    /// <summary>
    /// Whether a value of this type can be by-ref-like: one that is, or a type parameter whose constraint admits
    /// one. An open declaration is judged on what every substitution guarantees, so a constraint that admits a ref
    /// struct is answered like a ref struct. A compiler that cannot express the constraint cannot be given a
    /// declaration carrying it either, which is what the older targets fall back to.
    /// </summary>
    public static bool MayBeRefLike(ITypeSymbol type)
    {
#if CODE_ANALYSIS_4_12
        if (type is ITypeParameterSymbol { AllowsRefLikeType: true })
        {
            return true;
        }
#endif
        return IsRefLikeType(type);
    }

    // ref structs are C# 7.2, but no public symbol carries IsRefLikeType until Roslyn 3.0; the oldest band reads
    // the internal property behind it instead.
    private static bool IsRefLikeType(ITypeSymbol type)
#if CODE_ANALYSIS_3_0
        => type.IsRefLikeType;
#else
        => RefLikeTypePolyfill.IsRefLikeType(type);
#endif

    /// <summary>
    /// Names which of the two <see cref="MayBeRefLike"/> found, so a message reads the same from either source analyzer.
    /// </summary>
    public static string ByRefLikeClause(ITypeSymbol type)
        => IsRefLikeType(type) ? "is a ref struct" : "admits a ref struct";

    /// <summary>
    /// Which of the two rules applies. A ref struct is one the compiler can see, so the source cannot work; a
    /// constraint that merely admits one is decided by the type argument, and one that is not by-ref-like reads
    /// perfectly well - so the two are separate ids, configurable apart.
    /// </summary>
    public static DiagnosticDescriptor ByRefLikeRule(ITypeSymbol type)
        => IsRefLikeType(type) ? SourceElementMustNotBeByRefLikeRule : SourceElementMayBeByRefLikeRule;

    /// <summary>
    /// Whether <paramref name="type"/> is <paramref name="baseType"/> or derives from it. BenchmarkDotNet resolves
    /// its attributes with Type.GetCustomAttributes, which matches derived attribute types, so an analyzer comparing
    /// for exact identity would disagree with the runtime about e.g. a user's `MyParamsAttribute : ParamsAttribute`.
    /// </summary>
    public static bool IsOrDerivesFrom(ITypeSymbol? type, INamedTypeSymbol? baseType)
    {
        if (type == null || baseType == null)
        {
            return false;
        }
        for (ITypeSymbol? current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }
        return false;
    }

    public static bool AttributeListsContainAttribute(INamedTypeSymbol? attributeTypeSymbol, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
    {
        if (attributeTypeSymbol == null || attributeTypeSymbol.TypeKind == TypeKind.Error)
        {
            return false;
        }

        foreach (var attributeListSyntax in attributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeSyntaxTypeSymbol = semanticModel.GetTypeInfo(attributeSyntax).Type;
                if (attributeSyntaxTypeSymbol == null)
                {
                    continue;
                }

                if (IsOrDerivesFrom(attributeSyntaxTypeSymbol, attributeTypeSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool AttributeListContainsAttribute(string attributeName, Compilation compilation, ImmutableArray<AttributeData> attributeList)
        => AttributeListContainsAttribute(compilation.GetTypeByMetadataName(attributeName), attributeList);

    public static bool AttributeListContainsAttribute(INamedTypeSymbol? attributeTypeSymbol, ImmutableArray<AttributeData> attributeList)
    {
        if (attributeTypeSymbol == null || attributeTypeSymbol.TypeKind == TypeKind.Error)
        {
            return false;
        }

        return attributeList.Any(ad => IsOrDerivesFrom(ad.AttributeClass, attributeTypeSymbol));
    }

    public static ImmutableArray<AttributeSyntax> GetAttributes(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
        => GetAttributes(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

    public static ImmutableArray<AttributeSyntax> GetAttributes(INamedTypeSymbol? attributeTypeSymbol, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
    {
        var attributesBuilder = ImmutableArray.CreateBuilder<AttributeSyntax>();

        if (attributeTypeSymbol == null)
        {
            return attributesBuilder.ToImmutable();
        }

        foreach (var attributeListSyntax in attributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var attributeSyntaxTypeSymbol = semanticModel.GetTypeInfo(attributeSyntax).Type;
                if (attributeSyntaxTypeSymbol == null)
                {
                    continue;
                }

                if (IsOrDerivesFrom(attributeSyntaxTypeSymbol, attributeTypeSymbol))
                {
                    attributesBuilder.Add(attributeSyntax);
                }
            }
        }

        return attributesBuilder.ToImmutable();
    }

    public static string NormalizeTypeName(INamedTypeSymbol namedTypeSymbol)
    {
        string typeName;

        if (namedTypeSymbol.SpecialType != SpecialType.None)
        {
            typeName = namedTypeSymbol.ToString();
        }
        else if (namedTypeSymbol.IsUnboundGenericType)
        {
            typeName = $"{namedTypeSymbol.Name}<{new string(',', namedTypeSymbol.TypeArguments.Length - 1)}>";
        }
        else
        {
            typeName = namedTypeSymbol.Name;
        }

        return typeName;
    }

    public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
    {
        key = tuple.Key;
        value = tuple.Value;
    }

    public static Location GetLocation(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference?.SyntaxTree.GetLocation(attributeData.ApplicationSyntaxReference.Span)
            ?? Location.None;

    /// <summary>
    /// The source-name argument of a [ParamsSource]/[ArgumentsSource] usage - <c>nameof(Values)</c> in
    /// <c>[ArgumentsSource(nameof(Values))]</c>, or the second argument of the typeof-qualified form. Every rule
    /// about the named source reports here, so the squiggle sits on the member to change rather than on the whole
    /// attribute. Falls back to the attribute when the argument cannot be located.
    /// </summary>
    public static Location GetSourceNameLocation(this AttributeData attributeData)
    {
        if (attributeData.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax { ArgumentList: { } argumentList })
        {
            // By name first: ConstructorArguments is in parameter order and the syntax list in source order, which
            // coincide only while every argument is positional. A named one may sit anywhere.
            foreach (var argument in argumentList.Arguments)
            {
                if (argument.NameColon?.Name.Identifier.ValueText == "name")
                {
                    return argument.Expression.GetLocation();
                }
            }

            // All positional, so the name is the last argument - after the type where one is given.
            int nameIndex = attributeData.ConstructorArguments.Length == 2 ? 1 : 0;
            if (argumentList.Arguments.Count > nameIndex && argumentList.Arguments[nameIndex].NameColon is null)
            {
                return argumentList.Arguments[nameIndex].Expression.GetLocation();
            }
        }
        return attributeData.GetLocation();
    }

    /// <summary>
    /// Finds a [ParamsSource]/[ArgumentsSource] source member (method or property) by name, searching the type
    /// and its base types. Mirrors the runtime resolution, which uses GetAllMethods/GetAllProperties (inherited
    /// members included), unlike ITypeSymbol.GetMembers which only returns declared members.
    /// </summary>
    public static ISymbol? FindSourceMember(ITypeSymbol type, string name)
    {
        ISymbol? readableProperty = null;
        ISymbol? writeOnlyProperty = null;
        ISymbol? otherMethod = null;

        for (ITypeSymbol? current = type; current != null; current = current.BaseType)
        {
            foreach (var member in current.GetMembers(name))
            {
                switch (member)
                {
                    // What the runtime invokes: the first public non-generic method whose parameters are all
                    // optional. A generic overload is passed over here as it is there, so the rules that read the
                    // source's return type read the one that will actually be called.
                    case IMethodSymbol { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public, IsGenericMethod: false } method
                        when method.Parameters.All(parameter => parameter.IsOptional):
                        return method;

                    // The runtime falls back to a property with a public getter, so one of those wins wherever it is
                    // found - a write-only property nearer the derived end does not hide it, as taking the first of
                    // either kind would have made it do.
                    case IPropertySymbol { GetMethod.DeclaredAccessibility: Accessibility.Public }:
                        readableProperty ??= member;
                        break;

                    // A write-only property is nothing the runtime would read, but it is still the member the name
                    // most likely meant, so it is returned when nothing better turns up and BDN1305 reports it.
                    case IPropertySymbol:
                        writeOnlyProperty ??= member;
                        break;

                    // Nothing the runtime would use, but reporting on it beats reporting nothing.
                    case IMethodSymbol { MethodKind: MethodKind.Ordinary }:
                        otherMethod ??= member;
                        break;
                }
            }
        }

        return readableProperty ?? writeOnlyProperty ?? otherMethod;
    }

    public static bool IsAssignable(TypedConstant constant, ExpressionSyntax expression, ITypeSymbol targetType, Compilation compilation)
    {
        if (constant.IsNull)
        {
            // Check if targetType is a reference type or nullable.
            return targetType.IsReferenceType || targetType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        var sourceType = constant.Type;
        if (sourceType == null)
        {
            return false;
        }

        // Test if the constant type is implicitly assignable.
        var conversion = compilation.ClassifyConversion(sourceType, targetType);
        if (conversion.IsImplicit)
        {
            return true;
        }

        // Int32 values fail the test to smaller types, but it's still valid in the generated code to assign the literal to a smaller integer type,
        // so test if the expression is implicitly assignable.
        var semanticModel = compilation.GetSemanticModel(expression.SyntaxTree);
        // Only enums use explicit casting, so we test with explicit cast only for enums. See BenchmarkConverter.Map(...).
        bool isEnum = targetType.TypeKind == TypeKind.Enum;
        // The existing implementation only checks for direct enum type, not Nullable<TEnum>, so we won't check it here either unless BenchmarkConverter gets updated to handle it.
        //bool isNullableEnum =
        //    targetType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
        //    targetType is INamedTypeSymbol named &&
        //    named.TypeArguments.Length == 1 &&
        //    named.TypeArguments[0].TypeKind == TypeKind.Enum;
        conversion = semanticModel.ClassifyConversion(expression, targetType, isEnum);
        if (conversion.IsImplicit)
        {
            return true;
        }
        return isEnum && conversion.IsExplicit;
    }

    // Assumes a single `params object[] values` constructor
    public static ExpressionSyntax GetAttributeParamsArgumentExpression(this AttributeData attributeData, int index)
    {
        Debug.Assert(index >= 0);
        // Properties must come after constructor arguments, so we don't need to worry about it here.
        var attrSyntax = (AttributeSyntax)attributeData.ApplicationSyntaxReference!.GetSyntax();
        var args = attrSyntax.ArgumentList!.Arguments;
        Debug.Assert(args is { Count: > 0 });
        var maybeArrayExpression = args[0].Expression;

#if CODE_ANALYSIS_4_8
        if (maybeArrayExpression is CollectionExpressionSyntax collectionExpressionSyntax)
        {
            Debug.Assert(index < collectionExpressionSyntax.Elements.Count);
            return ((ExpressionElementSyntax)collectionExpressionSyntax.Elements[index]).Expression;
        }
#endif

        if (maybeArrayExpression is ArrayCreationExpressionSyntax arrayCreationExpressionSyntax)
        {
            if (arrayCreationExpressionSyntax.Initializer == null)
            {
                return maybeArrayExpression;
            }
            Debug.Assert(index < arrayCreationExpressionSyntax.Initializer.Expressions.Count);
            return arrayCreationExpressionSyntax.Initializer.Expressions[index];
        }

        // Params values
        Debug.Assert(index < args.Count);
        Debug.Assert(args[index].NameEquals is null);
        return args[index].Expression;
    }
}