using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    public static bool AttributeListsContainAttribute(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
        => AttributeListsContainAttribute(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

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

                if (attributeSyntaxTypeSymbol.Equals(attributeTypeSymbol))
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

        return attributeList.Any(ad => ad.AttributeClass != null && ad.AttributeClass.Equals(attributeTypeSymbol));
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

                if (attributeSyntaxTypeSymbol.Equals(attributeTypeSymbol))
                {
                    attributesBuilder.Add(attributeSyntax);
                }
            }
        }

        return attributesBuilder.ToImmutable();
    }

    public static int GetAttributeUsageCount(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
        => GetAttributeUsageCount(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

    public static int GetAttributeUsageCount(INamedTypeSymbol? attributeTypeSymbol, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
    {
        var attributeUsageCount = 0;

        if (attributeTypeSymbol == null)
        {
            return 0;
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

                if (attributeSyntaxTypeSymbol.Equals(attributeTypeSymbol))
                {
                    attributeUsageCount++;
                }
            }
        }

        return attributeUsageCount;
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

    public static bool IsAssignableToField(Compilation compilation, LanguageVersion languageVersion, string? valueTypeContainingNamespace, ITypeSymbol targetType, string valueExpression, Optional<object?> constantValue, string? valueType)
    {
        const string codeTemplate1 = """
            {0}
            
            file static class Internal {{
            static readonly {1} x = {2};
            }}
            """;

        const string codeTemplate2 = """
            {0}
            
            file static class Internal {{
            static readonly {1} x = ({2}){3};
            }}
            """;

        return IsAssignableTo(codeTemplate1, codeTemplate2, compilation, languageVersion, valueTypeContainingNamespace, targetType, valueExpression, constantValue, valueType);
    }

    public static bool IsAssignableToLocal(Compilation compilation, LanguageVersion languageVersion, string? valueTypeContainingNamespace, ITypeSymbol targetType, string valueExpression, Optional<object?> constantValue, string? valueType)
    {
        const string codeTemplate1 = """
            {0}
            
            file static class Internal {{
            static void Method() {{
                {1} x = {2};
            }}
            }}
            """;

        const string codeTemplate2 = """
            {0}
            
            file static class Internal {{
            static void Method() {{
                {1} x = ({2}){3};
            }}
            }}
            """;

        return IsAssignableTo(codeTemplate1, codeTemplate2, compilation, languageVersion, valueTypeContainingNamespace, targetType, valueExpression, constantValue, valueType);
    }

    private static bool IsAssignableTo(string codeTemplate1, string codeTemplate2, Compilation compilation, LanguageVersion languageVersion, string? valueTypeContainingNamespace, ITypeSymbol targetType, string valueExpression, Optional<object?> constantValue, string? valueType)
    {
        var usingDirective = valueTypeContainingNamespace != null ? $"using {valueTypeContainingNamespace};" : "";

        var hasNoCompilerDiagnostics = HasNoCompilerDiagnostics(string.Format(codeTemplate1, usingDirective, targetType, valueExpression), compilation, languageVersion);
        if (hasNoCompilerDiagnostics)
        {
            return true;
        }

        if (!constantValue.HasValue || valueType == null)
        {
            return false;
        }

        var constantLiteral = FormatLiteral(constantValue.Value);
        if (constantLiteral == null)
        {
            return false;
        }

        return HasNoCompilerDiagnostics(string.Format(codeTemplate2, usingDirective, targetType, valueType, constantLiteral), compilation, languageVersion);
    }

    private static bool HasNoCompilerDiagnostics(string code, Compilation compilation, LanguageVersion languageVersion)
    {
        var compilationTestSyntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(languageVersion));

        var syntaxTreesWithInterceptorsNamespaces = compilation.SyntaxTrees.Where(st => st.Options.Features.ContainsKey(InterceptorsNamespaces));

        var compilerDiagnostics = compilation
            .RemoveSyntaxTrees(syntaxTreesWithInterceptorsNamespaces)
            .AddSyntaxTrees(compilationTestSyntaxTree)
            .GetSemanticModel(compilationTestSyntaxTree)
            .GetMethodBodyDiagnostics()
            .Where(d => d.DefaultSeverity == DiagnosticSeverity.Error)
            .ToList();

        return compilerDiagnostics.Count == 0;
    }

    private static string? FormatLiteral(object? value)
    {
        return value switch
        {
            byte b => b.ToString(),
            sbyte sb => sb.ToString(),
            short s => s.ToString(),
            ushort us => us.ToString(),
            int i => i.ToString(),
            uint ui => $"{ui}U",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            float f => $"{f.ToString(CultureInfo.InvariantCulture)}F",
            double d => $"{d.ToString(CultureInfo.InvariantCulture)}D",
            decimal m => $"{m.ToString(CultureInfo.InvariantCulture)}M",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            string s => $"\"{s}\"",
            null => "null",
            _ => null
        };
    }

    public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
    {
        key = tuple.Key;
        value = tuple.Value;
    }
}