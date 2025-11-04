namespace BenchmarkDotNet.Analyzers.Attributes
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    using System;
    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArgumentsAttributeAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor RequiresBenchmarkAttributeRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute,
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute_Title)),
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute_MessageFormat)),
                                                                                                                "Usage",
                                                                                                                DiagnosticSeverity.Error,
                                                                                                                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor MustHaveMatchingValueCountRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount,
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_Title)),
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_MessageFormat)),
                                                                                                                "Usage",
                                                                                                                DiagnosticSeverity.Error,
                                                                                                                isEnabledByDefault: true,
                                                                                                                description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_Description)));

        internal static readonly DiagnosticDescriptor MustHaveMatchingValueTypeRule = new DiagnosticDescriptor(DiagnosticIds.Attributes_ArgumentsAttribute_MustHaveMatchingValueType,
                                                                                                               AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_Title)),
                                                                                                               AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_MessageFormat)),
                                                                                                               "Usage",
                                                                                                               DiagnosticSeverity.Error,
                                                                                                               isEnabledByDefault: true,
                                                                                                               description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_Description)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            RequiresBenchmarkAttributeRule,
            MustHaveMatchingValueCountRule,
            MustHaveMatchingValueTypeRule
        ];

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

                ctx.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return;
            }

            var argumentsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsAttribute");
            var argumentsSourceAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsSourceAttribute");

            if (argumentsAttributeTypeSymbol == null || argumentsSourceAttributeTypeSymbol == null)
            {
                return;
            }

            var hasBenchmarkAttribute = AnalyzerHelper.AttributeListsContainAttribute(AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation), methodDeclarationSyntax.AttributeLists, context.SemanticModel);

            var argumentsAttributes = AnalyzerHelper.GetAttributes(argumentsAttributeTypeSymbol, methodDeclarationSyntax.AttributeLists, context.SemanticModel);
            if (argumentsAttributes.Length == 0)
            {
                return;
            }

            if (!hasBenchmarkAttribute)
            {
                foreach (var argumentsAttributeSyntax in argumentsAttributes)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RequiresBenchmarkAttributeRule, argumentsAttributeSyntax.GetLocation()));
                }

                return;
            }

            var methodParameterTypeSymbolsBuilder = ImmutableArray.CreateBuilder<ITypeSymbol?>(methodDeclarationSyntax.ParameterList.Parameters.Count);

            foreach (var parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
            {
                if (parameterSyntax.Type != null)
                {
                    var expectedParameterTypeSymbol = context.SemanticModel.GetTypeInfo(parameterSyntax.Type).Type;
                    if (expectedParameterTypeSymbol != null && expectedParameterTypeSymbol.TypeKind != TypeKind.Error)
                    {
                        methodParameterTypeSymbolsBuilder.Add(expectedParameterTypeSymbol);

                        continue;
                    }

                    methodParameterTypeSymbolsBuilder.Add(null);
                }
            }

            var methodParameterTypeSymbols = methodParameterTypeSymbolsBuilder.ToImmutable();

            foreach (var argumentsAttributeSyntax in argumentsAttributes)
            {
                if (argumentsAttributeSyntax.ArgumentList == null)
                {
                    if (methodDeclarationSyntax.ParameterList.Parameters.Count > 0)
                    {
                        ReportMustHaveMatchingValueCountDiagnostic(argumentsAttributeSyntax.GetLocation(), 0);
                    }
                }
                else if (!argumentsAttributeSyntax.ArgumentList.Arguments.Any())
                {
                    if (methodDeclarationSyntax.ParameterList.Parameters.Count > 0)
                    {
                        ReportMustHaveMatchingValueCountDiagnostic(argumentsAttributeSyntax.ArgumentList.GetLocation(), 0);
                    }
                }
                else
                {
                    // Check if this is an explicit params array creation

                    var attributeArgumentSyntax = argumentsAttributeSyntax.ArgumentList.Arguments.First();
                    if (attributeArgumentSyntax.NameEquals != null)
                    {
                        // Ignore named arguments, e.g. Priority
                        if (methodDeclarationSyntax.ParameterList.Parameters.Count > 0)
                        {
                            ReportMustHaveMatchingValueCountDiagnostic(attributeArgumentSyntax.GetLocation(), 0);
                        }
                    }

                    // Collection expression

                    else if (attributeArgumentSyntax.Expression is CollectionExpressionSyntax collectionExpressionSyntax)
                    {
                        if (methodDeclarationSyntax.ParameterList.Parameters.Count != collectionExpressionSyntax.Elements.Count)
                        {
                            ReportMustHaveMatchingValueCountDiagnostic(collectionExpressionSyntax.Elements.Count == 0
                                                                          ? collectionExpressionSyntax.GetLocation()
                                                                          : Location.Create(context.FilterTree, collectionExpressionSyntax.Elements.Span),
                                                                       collectionExpressionSyntax.Elements.Count);

                            continue;
                        }

                        ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(i => collectionExpressionSyntax.Elements[i] is ExpressionElementSyntax expressionElementSyntax ? expressionElementSyntax.Expression : null);
                    }

                    // Array creation expression
                    else
                    {
                        var attributeArgumentSyntaxValueType = context.SemanticModel.GetTypeInfo(attributeArgumentSyntax.Expression).Type;
                        if (attributeArgumentSyntaxValueType is IArrayTypeSymbol arrayTypeSymbol && arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Object)
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
                                            if (methodDeclarationSyntax.ParameterList.Parameters.Count > 0)
                                            {
                                                ReportMustHaveMatchingValueCountDiagnostic(literalExpressionSyntax.GetLocation(), 0);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (methodDeclarationSyntax.ParameterList.Parameters.Count != arrayCreationExpressionSyntax.Initializer.Expressions.Count)
                                    {
                                        ReportMustHaveMatchingValueCountDiagnostic(arrayCreationExpressionSyntax.Initializer.Expressions.Count == 0
                                                                                      ? arrayCreationExpressionSyntax.Initializer.GetLocation()
                                                                                      : Location.Create(context.FilterTree, arrayCreationExpressionSyntax.Initializer.Expressions.Span),
                                                                                   arrayCreationExpressionSyntax.Initializer.Expressions.Count);

                                        continue;
                                    }

                                    // ReSharper disable once PossibleNullReferenceException
                                    ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(i => arrayCreationExpressionSyntax.Initializer.Expressions[i]);
                                }
                            }
                        }
                        else
                        {
                            // Params values

                            var firstNamedArgumentIndex = IndexOfNamedArgument(argumentsAttributeSyntax.ArgumentList.Arguments);
                            if (firstNamedArgumentIndex > 0)
                            {
                                if (methodDeclarationSyntax.ParameterList.Parameters.Count != firstNamedArgumentIndex.Value)
                                {
                                    ReportMustHaveMatchingValueCountDiagnostic(Location.Create(context.FilterTree, TextSpan.FromBounds(argumentsAttributeSyntax.ArgumentList.Arguments.Span.Start, argumentsAttributeSyntax.ArgumentList.Arguments[firstNamedArgumentIndex.Value - 1].Span.End)),
                                                                               firstNamedArgumentIndex.Value);

                                    continue;
                                }

                                // ReSharper disable once PossibleNullReferenceException
                                ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(i => argumentsAttributeSyntax.ArgumentList.Arguments[i].Expression);
                            }
                            else
                            {
                                if (methodDeclarationSyntax.ParameterList.Parameters.Count != argumentsAttributeSyntax.ArgumentList.Arguments.Count)
                                {
                                    ReportMustHaveMatchingValueCountDiagnostic(Location.Create(context.FilterTree, argumentsAttributeSyntax.ArgumentList.Arguments.Span),
                                                                               argumentsAttributeSyntax.ArgumentList.Arguments.Count);

                                    continue;
                                }

                                // ReSharper disable once PossibleNullReferenceException
                                ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(i => argumentsAttributeSyntax.ArgumentList.Arguments[i].Expression);
                            }
                        }
                    }
                }
            }

            return;

            void ReportMustHaveMatchingValueCountDiagnostic(Location diagnosticLocation, int valueCount)
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveMatchingValueCountRule, diagnosticLocation,
                                                           methodDeclarationSyntax.ParameterList.Parameters.Count,
                                                           methodDeclarationSyntax.ParameterList.Parameters.Count == 1 ? "" : "s",
                                                           methodDeclarationSyntax.Identifier.ToString(),
                                                           valueCount));
            }

            void ReportIfNotImplicitlyConvertibleValueTypeDiagnostic(Func<int, ExpressionSyntax> valueExpressionSyntaxFunc)
            {
                for (var i = 0; i < methodParameterTypeSymbols.Length; i++)
                {
                    var methodParameterTypeSymbol = methodParameterTypeSymbols[i];
                    if (methodParameterTypeSymbol == null)
                    {
                        continue;
                    }

                    var valueExpressionSyntax = valueExpressionSyntaxFunc(i);
                    if (valueExpressionSyntax == null)
                    {
                        continue;
                    }

                    var valueExpressionString = valueExpressionSyntax.ToString();

                    var constantValue = context.SemanticModel.GetConstantValue(valueExpressionSyntax);

                    var actualValueTypeSymbol = context.SemanticModel.GetTypeInfo(valueExpressionSyntax).Type;
                    if (actualValueTypeSymbol != null && actualValueTypeSymbol.TypeKind != TypeKind.Error)
                    {
                        if (!AnalyzerHelper.IsAssignableToLocal(context.Compilation,
                                                                methodParameterTypeSymbol,
                                                                valueExpressionString,
                                                                constantValue,
                                                                actualValueTypeSymbol.ToString()))
                        {
                            ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(valueExpressionSyntax.GetLocation(),
                                                                                 valueExpressionSyntax.ToString(),
                                                                                 methodParameterTypeSymbol.ToString(),
                                                                                 actualValueTypeSymbol.ToString());
                        }
                    }
                    else
                    {
                        if (constantValue is { HasValue: true, Value: null })
                        {
                            if (!AnalyzerHelper.IsAssignableToLocal(context.Compilation,
                                                                    methodParameterTypeSymbol,
                                                                    valueExpressionString,
                                                                    constantValue,
                                                                    null))
                            {
                                ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(valueExpressionSyntax.GetLocation(),
                                                                                     valueExpressionString,
                                                                                     methodParameterTypeSymbol.ToString(),
                                                                                     "null");
                            }
                        }
                    }
                }

                return;

                void ReportValueTypeMustBeImplicitlyConvertibleDiagnostic(Location diagnosticLocation, string value, string expectedType, string actualType)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MustHaveMatchingValueTypeRule,
                                                               diagnosticLocation,
                                                               value,
                                                               expectedType,
                                                               actualType));
                }
            }
        }

        private static int? IndexOfNamedArgument(SeparatedSyntaxList<AttributeArgumentSyntax> attributeArguments)
        {
            var i = 0;

            foreach (var attributeArgumentSyntax in attributeArguments)
            {
                if (attributeArgumentSyntax.NameEquals != null)
                {
                    return i;
                }

                i++;
            }

            return null;
        }
    }
}
