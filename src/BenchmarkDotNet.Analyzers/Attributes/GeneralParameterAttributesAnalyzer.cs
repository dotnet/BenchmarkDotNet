using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GeneralParameterAttributesAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor MutuallyExclusiveOnFieldRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnField_Description)));

    internal static readonly DiagnosticDescriptor MutuallyExclusiveOnPropertyRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_MutuallyExclusiveOnProperty_Description)));

    internal static readonly DiagnosticDescriptor FieldMustBePublic = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_FieldMustBePublic,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_FieldMustBePublic_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_FieldMustBePublic_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_FieldMustBePublic_Description)));

    internal static readonly DiagnosticDescriptor PropertyMustBePublic = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_PropertyMustBePublic,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustBePublic_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustBePublic_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustBePublic_Description)));

    internal static readonly DiagnosticDescriptor NotValidOnReadonlyFieldRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_NotValidOnReadonlyField,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_NotValidOnReadonlyField_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_NotValidOnReadonlyField_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_NotValidOnReadonlyField_Description)));

    internal static readonly DiagnosticDescriptor NotValidOnConstantFieldRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_NotValidOnConstantField,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_NotValidOnConstantField_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_NotValidOnConstantField_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    internal static readonly DiagnosticDescriptor PropertyMustHavePublicSetterRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyMustHavePublicSetter_Description)));

    internal static readonly DiagnosticDescriptor PropertyCannotBeInitOnlyRule = new(
        DiagnosticIds.Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_GeneralParameterAttributes_PropertyCannotBeInitOnly_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        MutuallyExclusiveOnFieldRule,
        MutuallyExclusiveOnPropertyRule,
        FieldMustBePublic,
        PropertyMustBePublic,
        NotValidOnReadonlyFieldRule,
        NotValidOnConstantFieldRule,
        PropertyCannotBeInitOnlyRule,
        PropertyMustHavePublicSetterRule
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

            ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Attribute);
        });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AttributeSyntax attributeSyntax)
        {
            return;
        }

        if (!AllAttributeTypeSymbolsExist(context, out var paramsAttributeTypeSymbol, out var paramsSourceAttributeTypeSymbol, out var paramsAllValuesAttributeTypeSymbol))
        {
            return;
        }

        var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
        if (attributeSyntaxTypeSymbol == null
            || attributeSyntaxTypeSymbol.TypeKind == TypeKind.Error
            ||
                   (!attributeSyntaxTypeSymbol.Equals(paramsAttributeTypeSymbol, SymbolEqualityComparer.Default)
                 && !attributeSyntaxTypeSymbol.Equals(paramsSourceAttributeTypeSymbol, SymbolEqualityComparer.Default)
                 && !attributeSyntaxTypeSymbol.Equals(paramsAllValuesAttributeTypeSymbol, SymbolEqualityComparer.Default)))
        {
            return;
        }

        var attributeTarget = attributeSyntax.FirstAncestorOrSelf<SyntaxNode>(n => n is FieldDeclarationSyntax or PropertyDeclarationSyntax);
        if (attributeTarget == null)
        {
            return;
        }

        ImmutableArray<AttributeSyntax> declaredAttributes;
        bool fieldOrPropertyIsPublic;
        Location fieldConstModifierLocation = null;
        Location fieldReadonlyModifierLocation = null;
        string fieldOrPropertyIdentifier;
        Location propertyInitAccessorKeywordLocation = null;
        Location fieldOrPropertyIdentifierLocation;
        bool propertyIsMissingAssignableSetter = false;
        DiagnosticDescriptor fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule;
        DiagnosticDescriptor fieldOrPropertyMustBePublicDiagnosticRule;

        if (attributeTarget is FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            declaredAttributes = fieldDeclarationSyntax.AttributeLists.SelectMany(als => als.Attributes).ToImmutableArray();
            fieldOrPropertyIsPublic = fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);

            var fieldConstModifierIndex = fieldDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.ConstKeyword);
            fieldConstModifierLocation = fieldConstModifierIndex >= 0 ? fieldDeclarationSyntax.Modifiers[fieldConstModifierIndex].GetLocation() : null;

            var fieldOrPropertyReadonlyModifierIndex = fieldDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
            fieldReadonlyModifierLocation = fieldOrPropertyReadonlyModifierIndex >= 0 ? fieldDeclarationSyntax.Modifiers[fieldOrPropertyReadonlyModifierIndex].GetLocation() : null;

            fieldOrPropertyIdentifier = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.ToString();
            fieldOrPropertyIdentifierLocation = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.GetLocation();
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule = MutuallyExclusiveOnFieldRule;
            fieldOrPropertyMustBePublicDiagnosticRule = FieldMustBePublic;
        }
        else if (attributeTarget is PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            declaredAttributes = propertyDeclarationSyntax.AttributeLists.SelectMany(als => als.Attributes).ToImmutableArray();
            fieldOrPropertyIsPublic = propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
            fieldOrPropertyIdentifier = propertyDeclarationSyntax.Identifier.ToString();

            var propertyInitAccessorIndex = propertyDeclarationSyntax.AccessorList?.Accessors.IndexOf(SyntaxKind.InitAccessorDeclaration);
            propertyInitAccessorKeywordLocation = propertyInitAccessorIndex >= 0 ? propertyDeclarationSyntax.AccessorList.Accessors[propertyInitAccessorIndex.Value].Keyword.GetLocation() : null;

            var propertySetAccessorIndex = propertyDeclarationSyntax.AccessorList?.Accessors.IndexOf(SyntaxKind.SetAccessorDeclaration);
            propertyIsMissingAssignableSetter = !propertySetAccessorIndex.HasValue || propertySetAccessorIndex.Value < 0 || propertyDeclarationSyntax.AccessorList.Accessors[propertySetAccessorIndex.Value].Modifiers.Any();

            fieldOrPropertyIdentifierLocation = propertyDeclarationSyntax.Identifier.GetLocation();
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule = MutuallyExclusiveOnPropertyRule;
            fieldOrPropertyMustBePublicDiagnosticRule = PropertyMustBePublic;
        }
        else
        {
            return;
        }

        AnalyzeFieldOrPropertySymbol(
            context,
            paramsAttributeTypeSymbol,
            paramsSourceAttributeTypeSymbol,
            paramsAllValuesAttributeTypeSymbol,
            declaredAttributes,
            fieldOrPropertyIsPublic,
            fieldConstModifierLocation,
            fieldReadonlyModifierLocation,
            fieldOrPropertyIdentifier,
            propertyInitAccessorKeywordLocation,
            propertyIsMissingAssignableSetter,
            fieldOrPropertyIdentifierLocation,
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
            fieldOrPropertyMustBePublicDiagnosticRule,
            attributeSyntax);
    }

    private static void AnalyzeFieldOrPropertySymbol(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol? paramsAttributeTypeSymbol,
        INamedTypeSymbol? paramsSourceAttributeTypeSymbol,
        INamedTypeSymbol? paramsAllValuesAttributeTypeSymbol,
        ImmutableArray<AttributeSyntax> declaredAttributes,
        bool fieldOrPropertyIsPublic,
        Location? fieldConstModifierLocation,
        Location? fieldReadonlyModifierLocation,
        string fieldOrPropertyIdentifier,
        Location? propertyInitAccessorKeywordLocation,
        bool propertyIsMissingAssignableSetter,
        Location fieldOrPropertyIdentifierLocation,
        DiagnosticDescriptor fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
        DiagnosticDescriptor fieldOrPropertyMustBePublicDiagnosticRule,
        AttributeSyntax attributeSyntax)
    {
        ImmutableArray<INamedTypeSymbol> applicableParameterAttributeTypeSymbols =
        [
            paramsAttributeTypeSymbol,
            paramsSourceAttributeTypeSymbol,
            paramsAllValuesAttributeTypeSymbol
        ];

        var parameterAttributeTypeSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var declaredAttributeSyntax in declaredAttributes)
        {
            var declaredAttributeTypeSymbol = context.SemanticModel.GetTypeInfo(declaredAttributeSyntax).Type;
            if (declaredAttributeTypeSymbol != null)
            {
                foreach (var applicableParameterAttributeTypeSymbol in applicableParameterAttributeTypeSymbols)
                {
                    if (declaredAttributeTypeSymbol.Equals(applicableParameterAttributeTypeSymbol, SymbolEqualityComparer.Default))
                    {
                        if (!parameterAttributeTypeSymbols.Add(applicableParameterAttributeTypeSymbol))
                        {
                            return;
                        }
                    }
                }
            }
        }

        if (parameterAttributeTypeSymbols.Count == 0)
        {
            return;
        }

        if (parameterAttributeTypeSymbols.Count != 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
                attributeSyntax.GetLocation(),
                fieldOrPropertyIdentifier)
            );

            return;
        }

        if (fieldConstModifierLocation != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(NotValidOnConstantFieldRule,
                fieldConstModifierLocation,
                attributeSyntax.Name.ToString())
            );

            return;
        }

        if (!fieldOrPropertyIsPublic)
        {
            context.ReportDiagnostic(Diagnostic.Create(fieldOrPropertyMustBePublicDiagnosticRule,
                fieldOrPropertyIdentifierLocation,
                fieldOrPropertyIdentifier,
                attributeSyntax.Name.ToString())
            );
        }

        if (fieldReadonlyModifierLocation != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(NotValidOnReadonlyFieldRule,
                fieldReadonlyModifierLocation,
                fieldOrPropertyIdentifier,
                attributeSyntax.Name.ToString())
            );
        }

        if (propertyInitAccessorKeywordLocation != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(PropertyCannotBeInitOnlyRule,
                propertyInitAccessorKeywordLocation,
                fieldOrPropertyIdentifier,
                attributeSyntax.Name.ToString())
            );
        }
        else if (propertyIsMissingAssignableSetter)
        {
            context.ReportDiagnostic(Diagnostic.Create(PropertyMustHavePublicSetterRule,
                fieldOrPropertyIdentifierLocation,
                fieldOrPropertyIdentifier,
                attributeSyntax.Name.ToString())
            );
        }
    }

    private static bool AllAttributeTypeSymbolsExist(
        in SyntaxNodeAnalysisContext context,
        out INamedTypeSymbol? paramsAttributeTypeSymbol,
        out INamedTypeSymbol? paramsSourceAttributeTypeSymbol,
        out INamedTypeSymbol? paramsAllValuesAttributeTypeSymbol)
    {
        paramsAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsAttribute");
        if (paramsAttributeTypeSymbol == null)
        {
            paramsSourceAttributeTypeSymbol = null;
            paramsAllValuesAttributeTypeSymbol = null;

            return false;
        }

        paramsSourceAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsSourceAttribute");
        if (paramsSourceAttributeTypeSymbol == null)
        {
            paramsAllValuesAttributeTypeSymbol = null;

            return false;
        }

        paramsAllValuesAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsAllValuesAttribute");
        if (paramsAllValuesAttributeTypeSymbol == null)
        {
            return false;
        }

        return true;
    }
}