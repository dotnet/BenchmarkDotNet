using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ParamsAllValuesAttributeAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor NotAllowedOnFlagsEnumPropertyOrFieldTypeRule = new(
        DiagnosticIds.Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAllValuesAttribute_NotAllowedOnFlagsEnumPropertyOrFieldType_Description)));

    internal static readonly DiagnosticDescriptor PropertyOrFieldTypeMustBeEnumOrBoolRule = new(
        DiagnosticIds.Attributes_ParamsAllValuesAttribute_PropertyOrFieldTypeMustBeEnumOrBool,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAllValuesAttribute_PropertyOrFieldTypeMustBeEnumOrBool_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsAllValuesAttribute_PropertyOrFieldTypeMustBeEnumOrBool_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        NotAllowedOnFlagsEnumPropertyOrFieldTypeRule,
        PropertyOrFieldTypeMustBeEnumOrBoolRule,
    }.ToImmutableArray();

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.EnableConcurrentExecution();
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        analysisContext.RegisterCompilationStartAction(ctx =>
        {
            // Only run if BenchmarkDotNet.Annotations is referenced
            if (GetParamsAllValuesAttributeTypeSymbol(ctx.Compilation) == null)
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

        var paramsAllValuesAttributeTypeSymbol = GetParamsAllValuesAttributeTypeSymbol(context.Compilation);

        var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
        if (attributeSyntaxTypeSymbol == null || !attributeSyntaxTypeSymbol.Equals(paramsAllValuesAttributeTypeSymbol))
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

        AnalyzeFieldOrPropertyTypeSyntax(context, fieldOrPropertyTypeSyntax);
    }

    private static void AnalyzeFieldOrPropertyTypeSyntax(SyntaxNodeAnalysisContext context, TypeSyntax fieldOrPropertyTypeSyntax)
    {
        if (fieldOrPropertyTypeSyntax is NullableTypeSyntax fieldOrPropertyNullableTypeSyntax)
        {
            fieldOrPropertyTypeSyntax = fieldOrPropertyNullableTypeSyntax.ElementType;
        }

        var fieldOrPropertyTypeSymbol = context.SemanticModel.GetTypeInfo(fieldOrPropertyTypeSyntax).Type;
        if (fieldOrPropertyTypeSymbol == null || fieldOrPropertyTypeSymbol.TypeKind == TypeKind.Error)
        {
            return;
        }

        if (fieldOrPropertyTypeSymbol.TypeKind == TypeKind.Enum)
        {
            var flagsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("System.FlagsAttribute");
            if (flagsAttributeTypeSymbol == null)
            {
                return;
            }

            if (fieldOrPropertyTypeSymbol.GetAttributes().Any(ad => ad.AttributeClass != null && ad.AttributeClass.Equals(flagsAttributeTypeSymbol)))
            {
                context.ReportDiagnostic(Diagnostic.Create(NotAllowedOnFlagsEnumPropertyOrFieldTypeRule, fieldOrPropertyTypeSyntax.GetLocation(), fieldOrPropertyTypeSymbol.ToString()));
            }

            return;
        }

        if (fieldOrPropertyTypeSymbol.SpecialType != SpecialType.System_Boolean)
        {
            context.ReportDiagnostic(Diagnostic.Create(PropertyOrFieldTypeMustBeEnumOrBoolRule, fieldOrPropertyTypeSyntax.GetLocation()));
        }
    }

    private static INamedTypeSymbol? GetParamsAllValuesAttributeTypeSymbol(Compilation compilation) => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsAllValuesAttribute");
}