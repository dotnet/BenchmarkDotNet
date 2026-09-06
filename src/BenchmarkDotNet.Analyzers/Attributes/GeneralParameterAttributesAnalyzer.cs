using BenchmarkDotNet.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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

    internal static readonly DiagnosticDescriptor ParamsSourceCannotUseWriteOnlyPropertyRule = new(
        DiagnosticIds.Attributes_ParamsSourceAttribute_CannotUseWriteOnlyProperty,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_CannotUseWriteOnlyProperty_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_CannotUseWriteOnlyProperty_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_CannotUseWriteOnlyProperty_Description)));

    internal static readonly DiagnosticDescriptor ParamsSourceMustReturnEnumerableRule = new(
        DiagnosticIds.Attributes_ParamsSourceAttribute_MustReturnEnumerable,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_MustReturnEnumerable_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_MustReturnEnumerable_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ParamsSourceAttribute_MustReturnEnumerable_Description)));

    internal static readonly DiagnosticDescriptor ReservedMemberNameRule = new(
        DiagnosticIds.General_ReservedMemberName,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_ReservedMemberName_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_ReservedMemberName_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_ReservedMemberName_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        MutuallyExclusiveOnFieldRule,
        MutuallyExclusiveOnPropertyRule,
        FieldMustBePublic,
        PropertyMustBePublic,
        NotValidOnReadonlyFieldRule,
        NotValidOnConstantFieldRule,
        PropertyMustHavePublicSetterRule,
        ParamsSourceCannotUseWriteOnlyPropertyRule,
        ParamsSourceMustReturnEnumerableRule,
        ReservedMemberNameRule,
        AnalyzerHelper.SourceMethodMustNotHaveRequiredParametersRule,
        AnalyzerHelper.SourceMethodMustNotBeGenericRule,
        AnalyzerHelper.SourceMustNotBeAmbiguouslyEnumerableRule,
        AnalyzerHelper.SourceElementMustNotBeByRefLikeRule,
        AnalyzerHelper.SourceElementMayBeByRefLikeRule,
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

        if (!AllAttributeTypeSymbolsExist(context, out var paramsAttributeTypeSymbol, out var paramsSourceAttributeTypeSymbol, out var paramsAllValuesAttributeTypeSymbol))
        {
            return;
        }

        var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
        if (attributeSyntaxTypeSymbol == null
            || attributeSyntaxTypeSymbol.TypeKind == TypeKind.Error
            ||
                   (!AnalyzerHelper.IsOrDerivesFrom(attributeSyntaxTypeSymbol, paramsAttributeTypeSymbol)
                 && !AnalyzerHelper.IsOrDerivesFrom(attributeSyntaxTypeSymbol, paramsSourceAttributeTypeSymbol)
                 && !AnalyzerHelper.IsOrDerivesFrom(attributeSyntaxTypeSymbol, paramsAllValuesAttributeTypeSymbol)))
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
        Location? fieldConstModifierLocation = null;
        Location? fieldReadonlyModifierLocation = null;
        string fieldOrPropertyIdentifier;
        Location fieldOrPropertyIdentifierLocation;
        // One field declaration can declare several names - `[Params] public int a, b;` applies the attribute to
        // both - and each is a member the runnable's object initializer has to bind.
        ImmutableArray<(string Name, Location Location)> declaredNames;
        bool propertyIsMissingAssignableSetter = false;
        bool fieldOrPropertyIsStatic;
        DiagnosticDescriptor fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule;
        DiagnosticDescriptor fieldOrPropertyMustBePublicDiagnosticRule;

        if (attributeTarget is FieldDeclarationSyntax fieldDeclarationSyntax)
        {
            declaredAttributes = fieldDeclarationSyntax.AttributeLists.SelectMany(als => als.Attributes).ToImmutableArray();
            fieldOrPropertyIsPublic = fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
            fieldOrPropertyIsStatic = fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);

            var fieldConstModifierIndex = fieldDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.ConstKeyword);
            fieldConstModifierLocation = fieldConstModifierIndex >= 0 ? fieldDeclarationSyntax.Modifiers[fieldConstModifierIndex].GetLocation() : null;

            var fieldOrPropertyReadonlyModifierIndex = fieldDeclarationSyntax.Modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
            fieldReadonlyModifierLocation = fieldOrPropertyReadonlyModifierIndex >= 0 ? fieldDeclarationSyntax.Modifiers[fieldOrPropertyReadonlyModifierIndex].GetLocation() : null;

            fieldOrPropertyIdentifier = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.ToString();
            fieldOrPropertyIdentifierLocation = fieldDeclarationSyntax.Declaration.Variables[0].Identifier.GetLocation();
            declaredNames = ImmutableArray.CreateRange(fieldDeclarationSyntax.Declaration.Variables
                .Select(variable => (variable.Identifier.ToString(), variable.Identifier.GetLocation())));
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule = MutuallyExclusiveOnFieldRule;
            fieldOrPropertyMustBePublicDiagnosticRule = FieldMustBePublic;
        }
        else if (attributeTarget is PropertyDeclarationSyntax propertyDeclarationSyntax)
        {
            declaredAttributes = propertyDeclarationSyntax.AttributeLists.SelectMany(als => als.Attributes).ToImmutableArray();
            fieldOrPropertyIsPublic = propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
            fieldOrPropertyIsStatic = propertyDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
            fieldOrPropertyIdentifier = propertyDeclarationSyntax.Identifier.ToString();

            // `init` counts: the runnable assigns parameters through an object initializer.
            var propertyAccessors = propertyDeclarationSyntax.AccessorList?.Accessors;
            propertyIsMissingAssignableSetter = !HasAssignableAccessor(propertyAccessors, SyntaxKind.SetAccessorDeclaration)
#if CODE_ANALYSIS_3_8
                && !HasAssignableAccessor(propertyAccessors, SyntaxKind.InitAccessorDeclaration)
#endif
                ;

            fieldOrPropertyIdentifierLocation = propertyDeclarationSyntax.Identifier.GetLocation();
            declaredNames = ImmutableArray.Create((fieldOrPropertyIdentifier, fieldOrPropertyIdentifierLocation));
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule = MutuallyExclusiveOnPropertyRule;
            fieldOrPropertyMustBePublicDiagnosticRule = PropertyMustBePublic;
        }
        else
        {
            return;
        }

        AnalyzeFieldOrPropertySymbol(
            context,
            paramsAttributeTypeSymbol!,
            paramsSourceAttributeTypeSymbol!,
            paramsAllValuesAttributeTypeSymbol!,
            declaredAttributes,
            fieldOrPropertyIsPublic,
            fieldConstModifierLocation,
            fieldReadonlyModifierLocation,
            fieldOrPropertyIdentifier,
            propertyIsMissingAssignableSetter,
            fieldOrPropertyIsStatic,
            fieldOrPropertyIdentifierLocation,
            declaredNames,
            fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
            fieldOrPropertyMustBePublicDiagnosticRule,
            attributeSyntax,
            attributeTarget);
    }

    private static void AnalyzeFieldOrPropertySymbol(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol paramsAttributeTypeSymbol,
        INamedTypeSymbol paramsSourceAttributeTypeSymbol,
        INamedTypeSymbol paramsAllValuesAttributeTypeSymbol,
        ImmutableArray<AttributeSyntax> declaredAttributes,
        bool fieldOrPropertyIsPublic,
        Location? fieldConstModifierLocation,
        Location? fieldReadonlyModifierLocation,
        string fieldOrPropertyIdentifier,
        bool propertyIsMissingAssignableSetter,
        bool fieldOrPropertyIsStatic,
        Location fieldOrPropertyIdentifierLocation,
        ImmutableArray<(string Name, Location Location)> declaredNames,
        DiagnosticDescriptor fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
        DiagnosticDescriptor fieldOrPropertyMustBePublicDiagnosticRule,
        AttributeSyntax attributeSyntax,
        SyntaxNode attributeTarget)
    {
        ImmutableArray<INamedTypeSymbol> applicableParameterAttributeTypeSymbols = ImmutableArray.Create(
            paramsAttributeTypeSymbol,
            paramsSourceAttributeTypeSymbol,
            paramsAllValuesAttributeTypeSymbol);

        // Counted by the attribute *class* written on the member, not by the parameter attribute it maps onto: a
        // derived attribute maps to the base it derives from, so two different classes deriving from the same one
        // are still two usages, matching how the runtime resolves them.
        var declaredParameterAttributeTypeSymbols = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var parameterAttributeTypeSymbols = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var declaredAttributeSyntax in declaredAttributes)
        {
            var declaredAttributeTypeSymbol = context.SemanticModel.GetTypeInfo(declaredAttributeSyntax).Type;
            if (declaredAttributeTypeSymbol != null)
            {
                foreach (var applicableParameterAttributeTypeSymbol in applicableParameterAttributeTypeSymbols)
                {
                    if (AnalyzerHelper.IsOrDerivesFrom(declaredAttributeTypeSymbol, applicableParameterAttributeTypeSymbol))
                    {
                        // Only the very same class applied twice is CS0579's to report, and repeating it here
                        // would say it twice. Two different classes in one family are not CS0579 - the compiler
                        // is silent about them - so they must reach the duplicate rule below instead of ending
                        // the analysis and taking every other diagnostic for this member with them.
                        if (!declaredParameterAttributeTypeSymbols.Add(declaredAttributeTypeSymbol))
                        {
                            return;
                        }

                        parameterAttributeTypeSymbols.Add(applicableParameterAttributeTypeSymbol);

                        // At most one family per attribute; the three are siblings today, and breaking keeps the
                        // count right if one ever derives from another.
                        break;
                    }
                }
            }
        }

        if (declaredParameterAttributeTypeSymbols.Count == 0)
        {
            return;
        }

        if (declaredParameterAttributeTypeSymbols.Count != 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(fieldOrPropertyCannotHaveMoreThanOneParameterAttributeAppliedDiagnosticRule,
                attributeSyntax.GetLocation(),
                fieldOrPropertyIdentifier)
            );

            return;
        }

        // The runnable derives from the benchmark type and assigns each instance parameter member through an object
        // initializer, which binds the member name unqualified. A parameter member named like a generated member (all
        // __-prefixed) therefore binds to the generated member and fails to compile. (Static parameters, sources,
        // arguments, and non-parameter members are reached via type-qualification/`base`/hiding and don't collide, so
        // only instance parameter members are checked.)
        // Every declared name, not only the first: the runtime reports each parameter member it cannot assign.
        if (!fieldOrPropertyIsStatic)
        {
            foreach (var (name, location) in declaredNames)
            {
                if (RunnableConstants.ReservedInstanceMemberNames.Contains(name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(ReservedMemberNameRule,
                        location,
                        name,
                        attributeSyntax.Name.ToString())
                    );
                }
            }
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

        if (propertyIsMissingAssignableSetter)
        {
            context.ReportDiagnostic(Diagnostic.Create(PropertyMustHavePublicSetterRule,
                fieldOrPropertyIdentifierLocation,
                fieldOrPropertyIdentifier,
                attributeSyntax.Name.ToString())
            );
        }

        if (parameterAttributeTypeSymbols.Contains(paramsSourceAttributeTypeSymbol))
        {
            AnalyzeParamsSourceWriteOnlyProperty(context, attributeSyntax, attributeTarget);
        }
    }

    private static void AnalyzeParamsSourceWriteOnlyProperty(
        SyntaxNodeAnalysisContext context,
        AttributeSyntax attributeSyntax,
        SyntaxNode attributeTarget)
    {
        ISymbol? symbol = attributeTarget switch
        {
            FieldDeclarationSyntax field => context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables[0]),
            PropertyDeclarationSyntax property => context.SemanticModel.GetDeclaredSymbol(property),
            _ => null
        };

        if (symbol == null)
        {
            return;
        }

        var attributeData = symbol.GetAttributes()
            .FirstOrDefault(attr => attr.ApplicationSyntaxReference?.GetSyntax() == attributeSyntax);

        if (attributeData == null)
        {
            return;
        }

        // These rules need the source's name, and the name is only in this usage's own constructor arguments when
        // [ParamsSource] itself was applied. A derived attribute may hand it to base(...), where the runtime still
        // reads it off the Name property but nothing here can see it - guessing from whatever arguments the derived
        // constructor happens to take would resolve some other member, or none.
        if (!SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, context.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.ParamsSourceAttribute")))
        {
            return;
        }

        string? sourceName = null;
        ITypeSymbol? targetType = null;

        // [ParamsSource("name")]
        if (attributeData.ConstructorArguments.Length == 1)
        {
            if (attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
                && attributeData.ConstructorArguments[0].Value is string name)
            {
                sourceName = name;
            }
            else
            {
                var syntax = (AttributeSyntax)attributeData.ApplicationSyntaxReference!.GetSyntax();
                if (syntax.ArgumentList?.Arguments.Count > 0)
                {
                    sourceName = ExtractNameFromExpression(syntax.ArgumentList.Arguments[0].Expression, context.SemanticModel);
                }
            }
            targetType = GetContainingType(attributeSyntax, context);
        }
        // [ParamsSource(typeof(OtherClass), nameof(OtherClass.Values))]
        else if (attributeData.ConstructorArguments.Length == 2)
        {
            if (attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Type
                && attributeData.ConstructorArguments[0].Value is ITypeSymbol type)
            {
                targetType = type;
            }

            if (attributeData.ConstructorArguments[1].Kind == TypedConstantKind.Primitive
                && attributeData.ConstructorArguments[1].Value is string name)
            {
                sourceName = name;
            }
            else
            {
                var syntax = (AttributeSyntax)attributeData.ApplicationSyntaxReference!.GetSyntax();
                if (syntax.ArgumentList?.Arguments.Count > 1)
                {
                    sourceName = ExtractNameFromExpression(syntax.ArgumentList.Arguments[1].Expression, context.SemanticModel);
                }
            }
        }

        if (string.IsNullOrEmpty(sourceName) || targetType == null)
        {
            return;
        }

        var referencedMember = AnalyzerHelper.FindSourceMember(targetType, sourceName!);

        Location location = attributeData.GetSourceNameLocation();

        if (referencedMember is IPropertySymbol propertySymbol
            && propertySymbol.SetMethod != null
            && propertySymbol.GetMethod == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ParamsSourceCannotUseWriteOnlyPropertyRule,
                location,
                sourceName));
            return;
        }

        if (AnalyzerHelper.SourceResolvesOnlyToGenericMethod(targetType, sourceName!))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AnalyzerHelper.SourceMethodMustNotBeGenericRule,
                location,
                sourceName));
            return;
        }

        if (AnalyzerHelper.SourceResolvesOnlyToRequiredParameterMethod(targetType, sourceName!))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AnalyzerHelper.SourceMethodMustNotHaveRequiredParametersRule,
                location,
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
                location,
                sourceName,
                returnType.ToDisplayString()));
        }
        else if (!AsyncTypeShapes.IsSupportedSourceReturnType(context.Compilation, returnType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ParamsSourceMustReturnEnumerableRule,
                location,
                sourceName,
                returnType.ToDisplayString()));
        }
        else if (AsyncTypeShapes.TryGetSourceElementType(context.Compilation, returnType, out var elementType)
            && AnalyzerHelper.MayBeRefLike(elementType!))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                AnalyzerHelper.ByRefLikeRule(elementType!),
                location,
                sourceName,
                elementType!.ToDisplayString(),
                AnalyzerHelper.ByRefLikeClause(elementType!)));
        }
    }

    // An accessor that an object initializer can assign through: `set` or `init`, without an accessibility
    // modifier (a non-public one can't be reached from the generated runnable).
    private static bool HasAssignableAccessor(SyntaxList<AccessorDeclarationSyntax>? accessors, SyntaxKind kind)
    {
        if (accessors is not { } list)
        {
            return false;
        }
        int index = list.IndexOf(kind);
        return index >= 0 && !list[index].Modifiers.Any();
    }

    private static string? ExtractNameFromExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        if (expression is InvocationExpressionSyntax invocation
            && invocation.Expression is IdentifierNameSyntax identifierName
            && identifierName.Identifier.ValueText == "nameof"
            && invocation.ArgumentList.Arguments.Count > 0)
        {
            var argumentExpression = invocation.ArgumentList.Arguments[0].Expression;
            var symbolInfo = semanticModel.GetSymbolInfo(argumentExpression);
            if (symbolInfo.Symbol != null)
            {
                return symbolInfo.Symbol.Name;
            }
            if (argumentExpression is IdentifierNameSyntax id)
            {
                return id.Identifier.ValueText;
            }
            if (argumentExpression is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Name.Identifier.ValueText;
            }
        }

        if (expression is LiteralExpressionSyntax literal
            && literal.Token.Value is string str)
        {
            return str;
        }

        return null;
    }

    private static INamedTypeSymbol? GetContainingType(AttributeSyntax attributeSyntax, SyntaxNodeAnalysisContext context)
    {
        var fieldOrProperty = attributeSyntax.FirstAncestorOrSelf<SyntaxNode>(n => n is FieldDeclarationSyntax or PropertyDeclarationSyntax);
        if (fieldOrProperty == null)
        {
            return null;
        }

        var containingType = fieldOrProperty.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType == null)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(containingType);
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