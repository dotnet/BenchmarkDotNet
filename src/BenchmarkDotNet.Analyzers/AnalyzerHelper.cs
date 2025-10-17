namespace BenchmarkDotNet.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using System.Collections.Immutable;
    using System.Linq;

    internal static class AnalyzerHelper
    {
        public static LocalizableResourceString GetResourceString(string name) => new(name, BenchmarkDotNetAnalyzerResources.ResourceManager, typeof(BenchmarkDotNetAnalyzerResources));

        public static INamedTypeSymbol? GetBenchmarkAttributeTypeSymbol(Compilation compilation) => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

        public static bool AttributeListsContainAttribute(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel) => AttributeListsContainAttribute(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

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

                    if (attributeSyntaxTypeSymbol.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool AttributeListContainsAttribute(string attributeName, Compilation compilation, ImmutableArray<AttributeData> attributeList) => AttributeListContainsAttribute(compilation.GetTypeByMetadataName(attributeName), attributeList);

        public static bool AttributeListContainsAttribute(INamedTypeSymbol? attributeTypeSymbol, ImmutableArray<AttributeData> attributeList)
        {
            if (attributeTypeSymbol == null || attributeTypeSymbol.TypeKind == TypeKind.Error)
            {
                return false;
            }

            return attributeList.Any(ad => ad.AttributeClass != null && ad.AttributeClass.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default));
        }

        public static ImmutableArray<AttributeSyntax> GetAttributes(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel) => GetAttributes(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

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

                    if (attributeSyntaxTypeSymbol.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default))
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

        public static bool IsAssignableToField(Compilation compilation, ITypeSymbol targetType, string valueExpression)
        {
            var code = $$"""
                         file static class Internal {
                            static readonly {{targetType}} x = {{valueExpression}};
                         }
                         """;

            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var compilationErrors = compilation.AddSyntaxTrees(syntaxTree)
                                               .GetSemanticModel(syntaxTree)
                                               .GetMethodBodyDiagnostics()
                                               .Where(d => d.DefaultSeverity == DiagnosticSeverity.Error)
                                               .ToList();

            return compilationErrors.Count == 0;
        }

        public static bool IsAssignableToLocal(Compilation compilation, ITypeSymbol targetType, string valueExpression)
        {
            var code = $$"""
                         file static class Internal {
                            static Internal() {
                                {{targetType}} x = {{valueExpression}};
                            }
                         }
                         """;

            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var compilationErrors = compilation.AddSyntaxTrees(syntaxTree)
                                               .GetSemanticModel(syntaxTree)
                                               .GetMethodBodyDiagnostics()
                                               .Where(d => d.DefaultSeverity == DiagnosticSeverity.Error)
                                               .ToList();

            return compilationErrors.Count == 0;
        }
    }
}
