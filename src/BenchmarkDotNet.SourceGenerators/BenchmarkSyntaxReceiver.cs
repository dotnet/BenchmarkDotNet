using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.SourceGenerators
{
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    internal class BenchmarkSyntaxReceiver : ISyntaxContextReceiver
    {
        private const string ArgumentsAttributeName = "BenchmarkDotNet.Attributes.ArgumentsAttribute";
        private const string ArgumentsSourceAttributeName = "BenchmarkDotNet.Attributes.ArgumentsSourceAttribute";
        private const string BenchmarkAttributeName = "BenchmarkDotNet.Attributes.BenchmarkAttribute";
        private const string GlobalCleanupAttributeName = "BenchmarkDotNet.Attributes.GlobalCleanupAttribute";
        private const string GlobalSetupAttributeName = "BenchmarkDotNet.Attributes.GlobalSetupAttribute";
        private const string IterationCleanupAttributeName = "BenchmarkDotNet.Attributes.IterationCleanupAttribute";
        private const string IterationSetupAttributeName = "BenchmarkDotNet.Attributes.IterationSetupAttribute";
        private const string ParamsAttributeName = "BenchmarkDotNet.Attributes.ParamsAttribute";
        private const string ParamsSourceAttributeName = "BenchmarkDotNet.Attributes.ParamsSourceAttribute";

        internal List<IFieldSymbol> ParamsFields { get; } = new List<IFieldSymbol>();
        internal List<IPropertySymbol> ParamsProperties { get; } = new List<IPropertySymbol>();
        internal List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

        internal IEnumerable<ISymbol> AllSymbols() => Methods.Concat<ISymbol>(ParamsFields).Concat(ParamsProperties);

        internal bool HasFound => Methods.Count > 0;

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            switch (context.Node)
            {
                case MethodDeclarationSyntax methodDeclarationSyntax when methodDeclarationSyntax.AttributeLists.Count > 0:
                    IMethodSymbol methodSymbol = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);

                    foreach (AttributeData attributeData in methodSymbol.GetAttributes())
                    {
                        switch (attributeData.AttributeClass.ToDisplayString())
                        {
                            case BenchmarkAttributeName:
                            case GlobalSetupAttributeName:
                            case GlobalCleanupAttributeName:
                            case IterationSetupAttributeName:
                            case IterationCleanupAttributeName:
                                Methods.Add(methodSymbol);
                                break;
                        }
                    }
                    break;
                case FieldDeclarationSyntax fieldDeclarationSyntax when fieldDeclarationSyntax.AttributeLists.Count > 0:
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        IFieldSymbol fieldSymbol = (IFieldSymbol)context.SemanticModel.GetDeclaredSymbol(variable);

                        if (fieldSymbol.GetAttributes().Any(attribute =>
                        {
                            string attributeName = attribute.AttributeClass.ToDisplayString();
                            return attributeName == ParamsAttributeName || attributeName == ParamsSourceAttributeName;
                        }))
                        {
                            ParamsFields.Add(fieldSymbol);
                        }
                    }
                    break;
                case PropertyDeclarationSyntax propertyDeclarationSyntax when propertyDeclarationSyntax.AttributeLists.Count > 0:
                    IPropertySymbol propertySymbol = (IPropertySymbol)context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);

                    if (propertySymbol.GetAttributes().Any(attribute =>
                    {
                        string attributeName = attribute.AttributeClass.ToDisplayString();
                        return attributeName == ParamsAttributeName || attributeName == ParamsSourceAttributeName;
                    }))
                    {
                        ParamsProperties.Add(propertySymbol);
                    }
                    break;
            }
        }
    }
}
