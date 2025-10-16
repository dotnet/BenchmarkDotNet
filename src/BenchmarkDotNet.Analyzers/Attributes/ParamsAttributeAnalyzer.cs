namespace BenchmarkDotNet.Analyzers.Attributes
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParamsAttributeAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor MustHaveValuesRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ParamsAttribute_MustHaveValues,
                                                                                                    AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveValues_Title)),
                                                                                                    AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveValues_MessageFormat)),
                                                                                                    "Usage",
                                                                                                    DiagnosticSeverity.Error,
                                                                                                    isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor UnexpectedValueTypeRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ParamsAttribute_UnexpectedValueType,
                                                                                                         AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnexpectedValueType_Title)),
                                                                                                         AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnexpectedValueType_MessageFormat)),
                                                                                                         "Usage",
                                                                                                         DiagnosticSeverity.Error,
                                                                                                         isEnabledByDefault: true,
                                                                                                         description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnexpectedValueType_Description)));

        internal static readonly DiagnosticDescriptor UnnecessarySingleValuePassedToAttributeRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute,
                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute_Title)),
                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute_MessageFormat)),
                                                                                                                             "Usage",
                                                                                                                             DiagnosticSeverity.Warning,
                                                                                                                             isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
                                                                                                           MustHaveValuesRule,
                                                                                                           UnexpectedValueTypeRule,
                                                                                                           UnnecessarySingleValuePassedToAttributeRule
                                                                                                          );

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);


            analysisContext.RegisterCompilationStartAction(ctx =>
            {
                // Only run if BenchmarkDotNet.Annotations is referenced
                var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(ctx.Compilation);
                if (benchmarkAttributeTypeSymbol == null)
                {
                    return;
                }

                ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Attribute);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeSyntax attributeSyntax)
            {
                return;
            }

            var paramsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsAttribute");
            if (paramsAttributeTypeSymbol == null)
            {
                return;
            }

            var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
            if (attributeSyntaxTypeSymbol == null || !attributeSyntaxTypeSymbol.Equals(paramsAttributeTypeSymbol, SymbolEqualityComparer.Default))
            {
                return;
            }

            var attributeTarget = attributeSyntax.FirstAncestorOrSelf<SyntaxNode>(n => n is FieldDeclarationSyntax or PropertyDeclarationSyntax);
            if (attributeTarget == null)
            {
                return;
            }

            TypeSyntax fieldOrPropertyTypeSyntax;

            if (attributeTarget is FieldDeclarationSyntax fieldDeclarationSyntax)
            {
                fieldOrPropertyTypeSyntax = fieldDeclarationSyntax.Declaration.Type;

            }
            else if (attributeTarget is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                fieldOrPropertyTypeSyntax = propertyDeclarationSyntax.Type;
            }
            else
            {
                return;
            }

            AnalyzeFieldOrPropertyTypeSyntax(context,
                                             fieldOrPropertyTypeSyntax,
                                             attributeSyntax);
        }

        private static void AnalyzeFieldOrPropertyTypeSyntax(SyntaxNodeAnalysisContext context,
                                                             TypeSyntax fieldOrPropertyTypeSyntax,
                                                             AttributeSyntax attributeSyntax)
        {
            if (attributeSyntax.ArgumentList == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                           attributeSyntax.GetLocation()));

                return;
            }

            if (!attributeSyntax.ArgumentList.Arguments.Any())
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                           attributeSyntax.ArgumentList.GetLocation()));

                return;
            }

            if (attributeSyntax.ArgumentList.Arguments.All(aas => aas.NameEquals != null))
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                           Location.Create(context.FilterTree, attributeSyntax.ArgumentList.Arguments.Span)));

                return;
            }

            var expectedValueTypeSymbol = context.SemanticModel.GetTypeInfo(fieldOrPropertyTypeSyntax).Type;
            if (expectedValueTypeSymbol == null || expectedValueTypeSymbol.TypeKind == TypeKind.Error)
            {
                return;
            }


            // Check if this is an explicit params array creation

            var attributeArgumentSyntax = attributeSyntax.ArgumentList.Arguments.First();
            if (attributeArgumentSyntax.NameEquals != null)
            {
                // Ignore named arguments, e.g. Priority
                return;
            }

            // Collection expression

            if (attributeArgumentSyntax.Expression is CollectionExpressionSyntax collectionExpressionSyntax)
            {
                if (!collectionExpressionSyntax.Elements.Any())
                {
                    context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                               collectionExpressionSyntax.GetLocation()));
                    return;
                }

                if (collectionExpressionSyntax.Elements.Count == 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(UnnecessarySingleValuePassedToAttributeRule,
                                                               collectionExpressionSyntax.Elements[0].GetLocation()));
                }

                foreach (var collectionElementSyntax in collectionExpressionSyntax.Elements)
                {
                    if (collectionElementSyntax is ExpressionElementSyntax expressionElementSyntax)
                    {
                        ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(expressionElementSyntax.Expression);
                    }
                }

                return;
            }

            // Array creation expression

            var attributeArgumentSyntaxValueType = context.SemanticModel.GetTypeInfo(attributeArgumentSyntax.Expression).Type;
            if (attributeArgumentSyntaxValueType is IArrayTypeSymbol arrayTypeSymbol)
            {
                if (arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Object)
                {
                    if (attributeArgumentSyntax.Expression is ArrayCreationExpressionSyntax arrayCreationExpressionSyntax)
                    {
                        if (arrayCreationExpressionSyntax.Initializer == null)
                        {
                            var rankSpecifierSizeSyntax = arrayCreationExpressionSyntax.Type.RankSpecifiers.First().Sizes.First();
                            if (rankSpecifierSizeSyntax is LiteralExpressionSyntax literalExpressionSyntax && literalExpressionSyntax.IsKind(SyntaxKind.NumericLiteralExpression))
                            {
                                if (literalExpressionSyntax.Token.Value is 0)
                                {
                                    context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                                               arrayCreationExpressionSyntax.GetLocation()));
                                }
                            }

                            return;
                        }

                        if (!arrayCreationExpressionSyntax.Initializer.Expressions.Any())
                        {
                            context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule,
                                                                       arrayCreationExpressionSyntax.Initializer.GetLocation()));

                            return;
                        }

                        if (arrayCreationExpressionSyntax.Initializer.Expressions.Count == 1)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(UnnecessarySingleValuePassedToAttributeRule,
                                                                       arrayCreationExpressionSyntax.Initializer.Expressions[0].GetLocation()));
                        }

                        foreach (var expressionSyntax in arrayCreationExpressionSyntax.Initializer.Expressions)
                        {
                            ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(expressionSyntax);
                        }
                    }
                }

                return;
            }


            // Params values

            if (attributeSyntax.ArgumentList.Arguments.Count(aas => aas.NameEquals == null) == 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnnecessarySingleValuePassedToAttributeRule,
                                                           attributeArgumentSyntax.Expression.GetLocation()));
            }

            foreach (var parameterValueAttributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
            {
                if (parameterValueAttributeArgumentSyntax.NameEquals != null)
                {
                    // Ignore named arguments, e.g. Priority
                    continue;
                }

                ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(parameterValueAttributeArgumentSyntax.Expression);
            }

            return;

            void ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(ExpressionSyntax valueExpressionSyntax)
            {
                var actualValueTypeSymbol = context.SemanticModel.GetTypeInfo(valueExpressionSyntax).Type;
                if (actualValueTypeSymbol != null && actualValueTypeSymbol.TypeKind != TypeKind.Error)
                {
                    var conversionSummary = context.Compilation.ClassifyConversion(actualValueTypeSymbol, expectedValueTypeSymbol);
                    if (!conversionSummary.IsImplicit)
                    {
                        if (conversionSummary is { IsExplicit: true, IsEnumeration: false })
                        {
                            var constantValue = context.SemanticModel.GetConstantValue(valueExpressionSyntax is CastExpressionSyntax castExpressionSyntax ? castExpressionSyntax.Expression : valueExpressionSyntax);
                            if (constantValue is { HasValue: true, Value: not null })
                            {
                                if (AnalyzerHelper.ValueFitsInType(constantValue.Value, expectedValueTypeSymbol))
                                {
                                    return;
                                }
                            }
                        }

                        ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(valueExpressionSyntax.GetLocation(),
                                                                             valueExpressionSyntax.ToString(),
                                                                             fieldOrPropertyTypeSyntax.ToString(),
                                                                             actualValueTypeSymbol.ToString());
                    }
                }
                else
                {
                    ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(valueExpressionSyntax.GetLocation(),
                                                                         valueExpressionSyntax.ToString(),
                                                                         fieldOrPropertyTypeSyntax.ToString());
                }

                return;

                void ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(Location diagnosticLocation, string value, string expectedType, string? actualType = null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(UnexpectedValueTypeRule,
                                                               diagnosticLocation,
                                                               value,
                                                               expectedType,
                                                               actualType ?? "<unknown>"));
                }
            }
        }
    }
}
