using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Analyzers;

internal static class AnalyzerHelper
{
    internal const string InterceptorsNamespaces = "InterceptorsNamespaces";

    public static LocalizableResourceString GetResourceString(string name)
        => new(name, BenchmarkDotNetAnalyzerResources.ResourceManager, typeof(BenchmarkDotNetAnalyzerResources));

    public static INamedTypeSymbol? GetBenchmarkAttributeTypeSymbol(Compilation compilation)
        => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

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

                if (SymbolEqualityComparer.Default.Equals(attributeSyntaxTypeSymbol, attributeTypeSymbol))
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

        return attributeList.Any(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, attributeTypeSymbol));
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

                if (SymbolEqualityComparer.Default.Equals(attributeSyntaxTypeSymbol, attributeTypeSymbol))
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