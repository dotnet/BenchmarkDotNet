namespace BenchmarkDotNet.Analyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    using System;
    using System.Collections.Immutable;

    internal static class AnalyzerHelper
    {
        public static LocalizableResourceString GetResourceString(string name) => new(name, BenchmarkDotNetAnalyzerResources.ResourceManager, typeof(BenchmarkDotNetAnalyzerResources));

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

        public static bool ValueFitsInType(object value, ITypeSymbol targetType)
        {


            try
            {
                switch (targetType.SpecialType)
                {
                    case SpecialType.System_Byte:
                        var byteVal = Convert.ToInt64(value);

                        return byteVal is >= byte.MinValue and <= byte.MaxValue;

                    case SpecialType.System_SByte:
                        var sbyteVal = Convert.ToInt64(value);

                        return sbyteVal is >= sbyte.MinValue and <= sbyte.MaxValue;

                    case SpecialType.System_Int16:
                        var int16Val = Convert.ToInt64(value);

                        return int16Val is >= short.MinValue and <= short.MaxValue;

                    case SpecialType.System_UInt16:
                        var uint16Val = Convert.ToInt64(value);

                        return uint16Val is >= ushort.MinValue and <= ushort.MaxValue;

                    case SpecialType.System_Int32:
                        var int32Val = Convert.ToInt64(value);

                        return int32Val is >= int.MinValue and <= int.MaxValue;

                    case SpecialType.System_UInt32:
                        var uint32Val = Convert.ToInt64(value);

                        return uint32Val is >= uint.MinValue and <= uint.MaxValue;

                    case SpecialType.System_Int64:
                        {
                            _ = Convert.ToInt64(value);
                        }

                        return true;

                    case SpecialType.System_UInt64:
                        var val = Convert.ToInt64(value);

                        return val >= 0;

                    case SpecialType.System_Single:
                        if (value is double)
                        {
                            return false;
                        }

                        var floatVal = Convert.ToSingle(value);

                        return !float.IsInfinity(floatVal);

                    case SpecialType.System_Double:
                        var doubleVal = Convert.ToDouble(value);

                        return !double.IsInfinity(doubleVal);

                    case SpecialType.System_Decimal:
                        if (value is double or float)
                        {
                            return false;
                        }

                        _ = Convert.ToDecimal(value);

                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
