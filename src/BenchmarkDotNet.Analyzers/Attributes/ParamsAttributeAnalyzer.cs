using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ParamsAttributeAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor MustHaveValuesRule = new(
        DiagnosticIds.Attributes_ParamsAttribute_MustHaveValues,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveValues_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveValues_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor MustHaveMatchingValueTypeRule = new(
        DiagnosticIds.Attributes_ParamsAttribute_MustHaveMatchingValueType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveMatchingValueType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveMatchingValueType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_MustHaveMatchingValueType_Description)));

    internal static readonly DiagnosticDescriptor UnnecessarySingleValuePassedToAttributeRule = new(
        DiagnosticIds.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAttribute_UnnecessarySingleValuePassedToAttribute_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        MustHaveValuesRule,
        MustHaveMatchingValueTypeRule,
        UnnecessarySingleValuePassedToAttributeRule,
    }.ToImmutableArray();

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.EnableConcurrentExecution();
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        analysisContext.RegisterCompilationStartAction(ctx =>
        {
            // Only run if BenchmarkDotNet.Annotations is referenced
            if (GetParamsAttributeTypeSymbol(ctx.Compilation) == null)
            {
                return;
            }

            ctx.RegisterSymbolAction(Analyze, SymbolKind.Field);
            ctx.RegisterSymbolAction(Analyze, SymbolKind.Property);
        });
    }

    private void Analyze(SymbolAnalysisContext context)
    {
        ITypeSymbol fieldOrPropertyType = context.Symbol switch
        {
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => null
        };
        if (fieldOrPropertyType is null)
        {
            return;
        }

        var paramsAttributeTypeSymbol = GetParamsAttributeTypeSymbol(context.Compilation);
        var attrs = context.Symbol.GetAttributes();
        var paramsAttributes = attrs.Where(attr => attr.AttributeClass.Equals(paramsAttributeTypeSymbol)).ToImmutableArray();
        if (paramsAttributes.Length != 1)
        {
            // Don't analyze zero or multiple [Params] (multiple is not legal and already handled by GeneralParameterAttributesAnalyzer).
            return;
        }

        var attr = paramsAttributes[0];

        // [Params]
        if (attr.ConstructorArguments.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule, attr.GetLocation()));
            return;
        }

        // [Params(null)]
        if (attr.ConstructorArguments[0].IsNull)
        {
            var syntax = (AttributeSyntax) attr.ApplicationSyntaxReference.GetSyntax();
            AnalyzeAssignableValueType(
                attr.ConstructorArguments[0],
                syntax.ArgumentList.Arguments[0].Expression,
                fieldOrPropertyType
            );
            return;
        }

        var actualValues = attr.ConstructorArguments[0].Values;

        // [Params([ ])]
        if (actualValues.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(MustHaveValuesRule, attr.GetLocation()));
            return;
        }

        // [Params(singleValue)]
        if (actualValues.Length == 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(UnnecessarySingleValuePassedToAttributeRule, AnalyzerHelper.GetAttributeParamsArgumentExpression(attr, 0).GetLocation()));
        }

        // [Params(multiple, values)]
        for (int i = 0; i < actualValues.Length; i++)
        {
            AnalyzeAssignableValueType(
                actualValues[i],
                AnalyzerHelper.GetAttributeParamsArgumentExpression(attr, i),
                fieldOrPropertyType
            );
        }

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
                    value.IsNull ? "null" : value.Type.ToDisplayString())
                );
            }
        }
    }

    private static INamedTypeSymbol? GetParamsAttributeTypeSymbol(Compilation compilation)
        => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsAttribute");
}