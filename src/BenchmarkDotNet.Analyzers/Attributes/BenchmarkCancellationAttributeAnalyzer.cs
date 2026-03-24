using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.Attributes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BenchmarkCancellationAttributeAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor MustBeCancellationTokenTypeRule = new(
        DiagnosticIds.Attributes_BenchmarkCancellationAttribute_MustBeCancellationTokenType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_MustBeCancellationTokenType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_MustBeCancellationTokenType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_MustBeCancellationTokenType_Description)));

    internal static readonly DiagnosticDescriptor FieldMustBePublicRule = new(
        DiagnosticIds.Attributes_BenchmarkCancellationAttribute_FieldMustBePublic,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_FieldMustBePublic_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_FieldMustBePublic_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_FieldMustBePublic_Description)));

    internal static readonly DiagnosticDescriptor PropertyMustBePublicRule = new(
        DiagnosticIds.Attributes_BenchmarkCancellationAttribute_PropertyMustBePublic,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustBePublic_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustBePublic_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustBePublic_Description)));

    internal static readonly DiagnosticDescriptor NotValidOnReadonlyFieldRule = new(
        DiagnosticIds.Attributes_BenchmarkCancellationAttribute_NotValidOnReadonlyField,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_NotValidOnReadonlyField_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_NotValidOnReadonlyField_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_NotValidOnReadonlyField_Description)));

    internal static readonly DiagnosticDescriptor PropertyMustHavePublicSetterRule = new(
        DiagnosticIds.Attributes_BenchmarkCancellationAttribute_PropertyMustHavePublicSetter,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustHavePublicSetter_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustHavePublicSetter_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_BenchmarkCancellationAttribute_PropertyMustHavePublicSetter_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        MustBeCancellationTokenTypeRule,
        FieldMustBePublicRule,
        PropertyMustBePublicRule,
        NotValidOnReadonlyFieldRule,
        PropertyMustHavePublicSetterRule,
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

            ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Attribute);
        });
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AttributeSyntax attributeSyntax)
        {
            return;
        }

        var benchmarkCancellationAttributeTypeSymbol = context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkCancellationAttribute");
        if (benchmarkCancellationAttributeTypeSymbol == null)
        {
            return;
        }

        var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
        if (attributeSyntaxTypeSymbol == null
            || attributeSyntaxTypeSymbol.TypeKind == TypeKind.Error
            || !SymbolEqualityComparer.Default.Equals(attributeSyntaxTypeSymbol, benchmarkCancellationAttributeTypeSymbol))
        {
            return;
        }

        var attributeTarget = attributeSyntax.FirstAncestorOrSelf<SyntaxNode>(n => n is FieldDeclarationSyntax or PropertyDeclarationSyntax);
        if (attributeTarget == null)
        {
            return;
        }

        var cancellationTokenTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (cancellationTokenTypeSymbol == null)
        {
            return;
        }

        if (attributeTarget is FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            AnalyzeField(context, fieldDeclarationSyntax, attributeSyntax, cancellationTokenTypeSymbol);
        }
        else if (attributeTarget is PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            AnalyzeProperty(context, propertyDeclarationSyntax, attributeSyntax, cancellationTokenTypeSymbol);
        }
    }

    private static void AnalyzeField(
        SyntaxNodeAnalysisContext context,
        FieldDeclarationSyntax fieldDeclarationSyntax,
        AttributeSyntax attributeSyntax,
        INamedTypeSymbol cancellationTokenTypeSymbol)
    {
        var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(fieldDeclarationSyntax.Declaration.Variables[0]) as IFieldSymbol;
        if (fieldSymbol == null)
        {
            return;
        }

        var fieldIdentifier = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.ToString();
        var fieldIdentifierLocation = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.GetLocation();

        // Check type
        if (!SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, cancellationTokenTypeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MustBeCancellationTokenTypeRule,
                fieldIdentifierLocation,
                fieldIdentifier,
                fieldSymbol.Type.ToDisplayString()));
        }

        // Check readonly
        if (fieldSymbol.IsReadOnly)
        {
            var readonlyModifierIndex = fieldDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
            var readonlyModifierLocation = readonlyModifierIndex >= 0
                ? fieldDeclarationSyntax.Modifiers[readonlyModifierIndex].GetLocation()
                : fieldIdentifierLocation;

            context.ReportDiagnostic(Diagnostic.Create(
                NotValidOnReadonlyFieldRule,
                readonlyModifierLocation,
                fieldIdentifier,
                attributeSyntax.Name.ToString()));
        }

        // Check public
        if (fieldSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FieldMustBePublicRule,
                fieldIdentifierLocation,
                fieldIdentifier,
                attributeSyntax.Name.ToString()));
        }
    }

    private static void AnalyzeProperty(
        SyntaxNodeAnalysisContext context,
        PropertyDeclarationSyntax propertyDeclarationSyntax,
        AttributeSyntax attributeSyntax,
        INamedTypeSymbol cancellationTokenTypeSymbol)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax) as IPropertySymbol;
        if (propertySymbol == null)
        {
            return;
        }

        var propertyIdentifier = propertyDeclarationSyntax.Identifier.ToString();
        var propertyIdentifierLocation = propertyDeclarationSyntax.Identifier.GetLocation();

        // Check type
        if (!SymbolEqualityComparer.Default.Equals(propertySymbol.Type, cancellationTokenTypeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MustBeCancellationTokenTypeRule,
                propertyIdentifierLocation,
                propertyIdentifier,
                propertySymbol.Type.ToDisplayString()));
        }

        // Check public
        if (propertySymbol.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PropertyMustBePublicRule,
                propertyIdentifierLocation,
                propertyIdentifier,
                attributeSyntax.Name.ToString()));
        }

        // Check setter
        if (propertySymbol.SetMethod == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PropertyMustHavePublicSetterRule,
                propertyIdentifierLocation,
                propertyIdentifier,
                attributeSyntax.Name.ToString()));
        }
        else if (propertySymbol.SetMethod.DeclaredAccessibility != Accessibility.Public)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PropertyMustHavePublicSetterRule,
                propertyIdentifierLocation,
                propertyIdentifier,
                attributeSyntax.Name.ToString()));
        }
    }
}
