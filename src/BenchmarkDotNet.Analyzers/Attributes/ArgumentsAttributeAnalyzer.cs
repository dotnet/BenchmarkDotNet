using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ArgumentsAttributeAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor RequiresBenchmarkAttributeRule = new(
        DiagnosticIds.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresBenchmarkAttribute_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MustHaveMatchingValueCountRule = new(
        DiagnosticIds.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueCount_Description)));

    internal static readonly DiagnosticDescriptor MustHaveMatchingValueTypeRule = new(
        DiagnosticIds.Attributes_ArgumentsAttribute_MustHaveMatchingValueType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_Description)));

    internal static readonly DiagnosticDescriptor RequiresParametersRule = new(
        DiagnosticIds.Attributes_ArgumentsAttribute_RequiresParameters,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresParameters_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresParameters_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_RequiresParameters_Description)));

    internal static readonly DiagnosticDescriptor ArgumentsSourceMustReturnEnumerableRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_MustReturnEnumerable,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustReturnEnumerable_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustReturnEnumerable_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustReturnEnumerable_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        RequiresBenchmarkAttributeRule,
        MustHaveMatchingValueCountRule,
        MustHaveMatchingValueTypeRule,
        RequiresParametersRule,
        ArgumentsSourceMustReturnEnumerableRule,
        AnalyzerHelper.SourceMethodMustNotHaveRequiredParametersRule,
        AnalyzerHelper.SourceMethodMustNotBeGenericRule,
        AnalyzerHelper.SourceElementMustNotBeByRefLikeRule,
        AnalyzerHelper.SourceElementMayBeByRefLikeRule,
        AnalyzerHelper.SourceMustNotBeAmbiguouslyEnumerableRule,
    }.ToImmutableArray();

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

            ctx.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
        });
    }

    private static void AnalyzeMethodSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
        var argumentsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsAttribute");
        var argumentsSourceAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ArgumentsSourceAttribute");

        if (argumentsAttributeTypeSymbol == null || argumentsSourceAttributeTypeSymbol == null)
        {
            return;
        }

        bool hasBenchmarkAttribute = false;
        var argumentsAttributes = new List<AttributeData>();
        var argumentsSourceAttributes = new List<AttributeData>();
        foreach (var attr in methodSymbol.GetAttributes())
        {
            if (AnalyzerHelper.IsOrDerivesFrom(attr.AttributeClass, benchmarkAttributeTypeSymbol))
            {
                hasBenchmarkAttribute = true;
            }
            else if (AnalyzerHelper.IsOrDerivesFrom(attr.AttributeClass, argumentsAttributeTypeSymbol))
            {
                argumentsAttributes.Add(attr);
            }
            else if (AnalyzerHelper.IsOrDerivesFrom(attr.AttributeClass, argumentsSourceAttributeTypeSymbol))
            {
                argumentsSourceAttributes.Add(attr);
            }
        }

        if (argumentsAttributes.Count == 0 && argumentsSourceAttributes.Count == 0)
        {
            return;
        }

        bool methodHasZeroParams = methodSymbol.Parameters.Length == 0;
        if (!hasBenchmarkAttribute || methodHasZeroParams)
        {
            argumentsAttributes.AddRange(argumentsSourceAttributes);
            foreach (var attr in argumentsAttributes)
            {
                if (!hasBenchmarkAttribute)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RequiresBenchmarkAttributeRule, attr.GetLocation()));
                }
                if (methodHasZeroParams)
                {
                    context.ReportDiagnostic(Diagnostic.Create(RequiresParametersRule, attr.GetLocation(), methodSymbol.Name));
                }
            }
            return;
        }

        foreach (var attr in argumentsAttributes)
        {
            // Only [Arguments] itself is guaranteed to carry the values in its own constructor arguments. A derived
            // attribute declares whatever constructor it likes and may hand values to base(...), where they are
            // invisible here, so its arguments are not the values to inspect.
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, argumentsAttributeTypeSymbol))
            {
                continue;
            }

            // [Arguments]
            if (attr.ConstructorArguments.Length == 0)
            {
                ReportMustHaveMatchingValueCountDiagnostic(attr.GetLocation(), 0);
                continue;
            }

            // [Arguments(null)]
            if (attr.ConstructorArguments[0].IsNull)
            {
                if (methodSymbol.Parameters.Length > 1)
                {
                    ReportMustHaveMatchingValueCountDiagnostic(attr.GetLocation(), 1);
                }
                else
                {
                    var syntax = (AttributeSyntax)attr.ApplicationSyntaxReference!.GetSyntax();
                    AnalyzeAssignableValueType(
                        attr.ConstructorArguments[0],
                        syntax.ArgumentList!.Arguments[0].Expression,
                        methodSymbol.Parameters[0].Type
                    );
                }
                continue;
            }

            // [Arguments(multiple, values)]
            var actualValues = attr.ConstructorArguments[0].Values;
            if (actualValues.Length != methodSymbol.Parameters.Length)
            {
                ReportMustHaveMatchingValueCountDiagnostic(attr.GetLocation(), actualValues.Length);
                continue;
            }

            for (int i = 0; i < actualValues.Length; i++)
            {
                AnalyzeAssignableValueType(
                    actualValues[i],
                    AnalyzerHelper.GetAttributeParamsArgumentExpression(attr, i),
                    methodSymbol.Parameters[i].Type
                );
            }
        }

        foreach (var attr in argumentsSourceAttributes)
        {
            AnalyzeArgumentsSourceReturnType(attr);
        }

        void AnalyzeArgumentsSourceReturnType(AttributeData attr)
        {
            // These rules need the source's name, which is in this usage's own arguments only when
            // [ArgumentsSource] itself was applied - a derived attribute may hand it to base(...), out of sight.
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, argumentsSourceAttributeTypeSymbol))
            {
                return;
            }

            // [ArgumentsSource(nameof(Source))] or [ArgumentsSource(typeof(Other), nameof(Other.Source))]
            ITypeSymbol? sourceType;
            string? sourceName;
            if (attr.ConstructorArguments.Length == 1)
            {
                sourceType = methodSymbol.ContainingType;
                sourceName = attr.ConstructorArguments[0].Value as string;
            }
            else if (attr.ConstructorArguments.Length == 2)
            {
                sourceType = attr.ConstructorArguments[0].Value as ITypeSymbol;
                sourceName = attr.ConstructorArguments[1].Value as string;
            }
            else
            {
                return;
            }

            if (sourceType == null || string.IsNullOrEmpty(sourceName))
            {
                return;
            }

            var referencedMember = AnalyzerHelper.FindSourceMember(sourceType, sourceName!);

            if (AnalyzerHelper.SourceResolvesOnlyToGenericMethod(sourceType, sourceName!))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AnalyzerHelper.SourceMethodMustNotBeGenericRule,
                    attr.GetSourceNameLocation(),
                    sourceName));
                return;
            }

            if (AnalyzerHelper.SourceResolvesOnlyToRequiredParameterMethod(sourceType, sourceName!))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AnalyzerHelper.SourceMethodMustNotHaveRequiredParametersRule,
                    attr.GetSourceNameLocation(),
                    sourceName));
                return;
            }

            ITypeSymbol? returnType = referencedMember switch
            {
                IMethodSymbol method => method.ReturnType,
                IPropertySymbol property => property.Type,
                _ => null
            };

            if (returnType == null || returnType.TypeKind == TypeKind.Error)
            {
                return;
            }

            if (AsyncTypeShapes.IsAmbiguouslyEnumerable(context.Compilation, returnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AnalyzerHelper.SourceMustNotBeAmbiguouslyEnumerableRule,
                    attr.GetSourceNameLocation(),
                    sourceName,
                    returnType.ToDisplayString()));
                return;
            }

            if (!AsyncTypeShapes.IsSupportedSourceReturnType(context.Compilation, returnType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ArgumentsSourceMustReturnEnumerableRule,
                    attr.GetSourceNameLocation(),
                    sourceName,
                    returnType.ToDisplayString()));
                return;
            }

            if (!AsyncTypeShapes.TryGetSourceElementType(context.Compilation, returnType, out var elementType))
            {
                return;
            }

            // Discovery reads the values into an object[], which a ref struct cannot enter. Expressible since .NET 10
            // gave IEnumerable<T> an allows-ref-struct type parameter; a ref struct *parameter* is still supported,
            // fed from whatever the value is built from. A constraint admitting one is answered the same way, as an
            // open declaration is judged on what every substitution guarantees.
            if (AnalyzerHelper.MayBeRefLike(elementType!))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    AnalyzerHelper.ByRefLikeRule(elementType!),
                    attr.GetSourceNameLocation(),
                    sourceName,
                    elementType!.ToDisplayString(),
                    AnalyzerHelper.ByRefLikeClause(elementType!)));
                return;
            }

        }

        void ReportMustHaveMatchingValueCountDiagnostic(Location diagnosticLocation, int valueCount)
            => context.ReportDiagnostic(Diagnostic.Create(MustHaveMatchingValueCountRule,
                diagnosticLocation,
                methodSymbol.Parameters.Length,
                methodSymbol.Parameters.Length == 1 ? "" : "s",
                methodSymbol.Name,
                valueCount)
            );

        void AnalyzeAssignableValueType(TypedConstant value, ExpressionSyntax expression, ITypeSymbol parameterType)
        {
            // Don't analyze unknown types.
            if (value.Kind == TypedConstantKind.Error || parameterType is IErrorTypeSymbol)
            {
                return;
            }
            if (!AnalyzerHelper.IsAssignable(value, expression, parameterType, context.Compilation))
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveMatchingValueTypeRule,
                    expression.GetLocation(),
                    expression.ToString(),
                    parameterType.ToDisplayString(),
                    value.IsNull ? "null" : value.Type!.ToDisplayString())
                );
            }
        }
    }
}