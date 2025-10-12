namespace BenchmarkDotNet.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using System.Collections.Immutable;

    internal static class AnalyzerHelper
    {
        public static LocalizableResourceString GetResourceString(string name) => new LocalizableResourceString(name, BenchmarkDotNetAnalyzerResources.ResourceManager, typeof(BenchmarkDotNetAnalyzerResources));

        public static INamedTypeSymbol? GetBenchmarkAttributeTypeSymbol(Compilation compilation) => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkAttribute");

        public static bool AttributeListsContainAttribute(string attributeName, Compilation compilation, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel) => AttributeListsContainAttribute(compilation.GetTypeByMetadataName(attributeName), attributeLists, semanticModel);

        public static bool AttributeListsContainAttribute(INamedTypeSymbol? attributeTypeSymbol, SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
        {
            if (attributeTypeSymbol == null)
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
    }
}
