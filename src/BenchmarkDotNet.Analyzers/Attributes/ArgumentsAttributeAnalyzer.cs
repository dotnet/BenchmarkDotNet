using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsAttribute_MustHaveMatchingValueType_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        RequiresBenchmarkAttributeRule,
        MustHaveMatchingValueCountRule,
        MustHaveMatchingValueTypeRule,
        RequiresParametersRule,
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
            if (attr.AttributeClass.Equals(benchmarkAttributeTypeSymbol))
            {
                hasBenchmarkAttribute = true;
            }
            else if (attr.AttributeClass.Equals(argumentsAttributeTypeSymbol))
            {
                argumentsAttributes.Add(attr);
            }
            else if (attr.AttributeClass.Equals(argumentsSourceAttributeTypeSymbol))
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
                    var syntax = (AttributeSyntax) attr.ApplicationSyntaxReference.GetSyntax();
                    AnalyzeAssignableValueType(
                        attr.ConstructorArguments[0],
                        syntax.ArgumentList.Arguments[0].Expression,
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
            if (!AnalyzerHelper.IsAssignable(value, expression, parameterType, context.Compilation))
            {
                context.ReportDiagnostic(Diagnostic.Create(MustHaveMatchingValueTypeRule,
                    expression.GetLocation(),
                    expression.ToString(),
                    parameterType.ToDisplayString(),
                    value.IsNull ? "null" : value.Type.ToDisplayString())
                );
            }
        }
    }
}