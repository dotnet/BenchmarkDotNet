namespace BenchmarkDotNet.Analyzers.General
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BenchmarkClassAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract,
                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_Title)),
                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_MessageFormat)),
                                                                                                                                             "Usage",
                                                                                                                                             DiagnosticSeverity.Error,
                                                                                                                                             isEnabledByDefault: true,
                                                                                                                                             description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_Description)));

        internal static readonly DiagnosticDescriptor ClassWithGenericTypeArgumentsAttributeMustBeGenericRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric,
                                                                                                                                         AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric_Title)),
                                                                                                                                         AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric_MessageFormat)),
                                                                                                                                         "Usage",
                                                                                                                                         DiagnosticSeverity.Error,
                                                                                                                                         isEnabledByDefault: true,
                                                                                                                                         description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeGeneric_Description)));

        internal static readonly DiagnosticDescriptor GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount,
                                                                                                                                                     AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount_Title)),
                                                                                                                                                     AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount_MessageFormat)),
                                                                                                                                                     "Usage",
                                                                                                                                                     DiagnosticSeverity.Error,
                                                                                                                                                     isEnabledByDefault: true,
                                                                                                                                                     description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCount_Description)));

        internal static readonly DiagnosticDescriptor MethodMustBePublicRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_MethodMustBePublic,
                                                                                                        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBePublic_Title)),
                                                                                                        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBePublic_MessageFormat)),
                                                                                                        "Usage",
                                                                                                        DiagnosticSeverity.Error,
                                                                                                        isEnabledByDefault: true,
                                                                                                        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBePublic_Description)));

        internal static readonly DiagnosticDescriptor MethodMustBeNonGenericRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_MethodMustBeNonGeneric,
                                                                                                            AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBeNonGeneric_Title)),
                                                                                                            AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBeNonGeneric_MessageFormat)),
                                                                                                            "Usage",
                                                                                                            DiagnosticSeverity.Error,
                                                                                                            isEnabledByDefault: true,
                                                                                                            description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_MethodMustBeNonGeneric_Description)));

        internal static readonly DiagnosticDescriptor ClassMustBeNonStaticRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassMustBeNonStatic,
                                                                                                          AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_Title)),
                                                                                                          AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_MessageFormat)),
                                                                                                          "Usage",
                                                                                                          DiagnosticSeverity.Error,
                                                                                                          isEnabledByDefault: true,
                                                                                                          description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_Description)));

        internal static readonly DiagnosticDescriptor SingleNullArgumentToBenchmarkCategoryAttributeNotAllowedRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_SingleNullArgumentToBenchmarkCategoryAttributeNotAllowed,
                                                                                                                                              AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_SingleNullArgumentToBenchmarkCategoryAttributeNotAllowed_Title)),
                                                                                                                                              AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_SingleNullArgumentToBenchmarkCategoryAttributeNotAllowed_MessageFormat)),
                                                                                                                                              "Usage",
                                                                                                                                              DiagnosticSeverity.Error,
                                                                                                                                              isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OnlyOneMethodCanBeBaselineRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_OnlyOneMethodCanBeBaseline,
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaseline_Title)),
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaseline_MessageFormat)),
                                                                                                                "Usage",
                                                                                                                DiagnosticSeverity.Error,
                                                                                                                isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor OnlyOneMethodCanBeBaselinePerCategoryRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_OnlyOneMethodCanBeBaselinePerCategory,
                                                                                                                           AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaselinePerCategory_Title)),
                                                                                                                           AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaselinePerCategory_MessageFormat)),
                                                                                                                           "Usage",
                                                                                                                           DiagnosticSeverity.Warning,
                                                                                                                           isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule,
            ClassWithGenericTypeArgumentsAttributeMustBeGenericRule,
            GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule,
            MethodMustBePublicRule,
            MethodMustBeNonGenericRule,
            ClassMustBeNonStaticRule,
            SingleNullArgumentToBenchmarkCategoryAttributeNotAllowedRule,
            OnlyOneMethodCanBeBaselineRule,
            OnlyOneMethodCanBeBaselinePerCategoryRule
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

                ctx.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
                ctx.RegisterSyntaxNodeAction(AnalyzeAttributeSyntax, SyntaxKind.Attribute);
            });
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            var classStaticModifier = null as SyntaxToken?;
            var classAbstractModifier = null as SyntaxToken?;

            foreach (var modifier in classDeclarationSyntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    classStaticModifier = modifier;
                }
                else if (modifier.IsKind(SyntaxKind.AbstractKeyword))
                {
                    classAbstractModifier = modifier;
                }
            }

            var genericTypeArgumentsAttributes = AnalyzerHelper.GetAttributes("BenchmarkDotNet.Attributes.GenericTypeArgumentsAttribute", context.Compilation, classDeclarationSyntax.AttributeLists, context.SemanticModel);
            if (genericTypeArgumentsAttributes.Length > 0 )
            {
                if (classAbstractModifier.HasValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule, classAbstractModifier.Value.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
                }

                foreach (var genericTypeArgumentsAttribute in genericTypeArgumentsAttributes)
                {
                    if (classDeclarationSyntax.TypeParameterList == null || classDeclarationSyntax.TypeParameterList.Parameters.Count == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ClassWithGenericTypeArgumentsAttributeMustBeGenericRule, genericTypeArgumentsAttribute.GetLocation()));
                    }
                    else if (genericTypeArgumentsAttribute.ArgumentList is { Arguments.Count: > 0 })
                    {
                        if (genericTypeArgumentsAttribute.ArgumentList.Arguments.Count != classDeclarationSyntax.TypeParameterList.Parameters.Count)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule, Location.Create(context.FilterTree, genericTypeArgumentsAttribute.ArgumentList.Arguments.Span),
                                                                       classDeclarationSyntax.TypeParameterList.Parameters.Count,
                                                                       classDeclarationSyntax.TypeParameterList.Parameters.Count == 1 ? "" : "s",
                                                                       classDeclarationSyntax.Identifier.ToString(),
                                                                       genericTypeArgumentsAttribute.ArgumentList.Arguments.Count));
                        }
                    }
                }
            }

            var benchmarkAttributeTypeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
            if (benchmarkAttributeTypeSymbol == null)
            {
                return;
            }

            var benchmarkCategoryAttributeTypeSymbol = GetBenchmarkCategoryTypeSymbol(context.Compilation);
            if (benchmarkCategoryAttributeTypeSymbol == null)
            {
                return;
            }

            var hasBenchmarkMethods = false;
            var nullBenchmarkCategoryBenchmarkAttributeBaselineLocations = new List<Location>();
            var benchmarkCategoryBenchmarkAttributeBaselineLocations = new Dictionary<string, List<Location>>();

            foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
            {
                var hasBenchmarkCategoryCompilerDiagnostics = false;
                var benchmarkCategories = new List<string?>();
                var benchmarkAttributeUsages = new List<AttributeSyntax>();

                if (memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
                {
                    foreach (var attributeListSyntax in methodDeclarationSyntax.AttributeLists)
                    {
                        foreach (var attributeSyntax in attributeListSyntax.Attributes)
                        {
                            var attributeSyntaxTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
                            if (attributeSyntaxTypeSymbol == null)
                            {
                                continue;
                            }

                            if (attributeSyntaxTypeSymbol.Equals(benchmarkAttributeTypeSymbol, SymbolEqualityComparer.Default))
                            {
                                benchmarkAttributeUsages.Add(attributeSyntax);
                            }
                            else if (attributeSyntaxTypeSymbol.Equals(benchmarkCategoryAttributeTypeSymbol, SymbolEqualityComparer.Default))
                            {
                                if (attributeSyntax.ArgumentList is { Arguments.Count: 1 })
                                {
                                    // Check if this is an explicit params array creation

                                    Optional<object?> constantValue;

                                    // Collection expression

                                    if (attributeSyntax.ArgumentList.Arguments[0].Expression is CollectionExpressionSyntax collectionExpressionSyntax)
                                    {
                                        foreach (var collectionElementSyntax in collectionExpressionSyntax.Elements)
                                        {
                                            if (collectionElementSyntax is ExpressionElementSyntax expressionElementSyntax)
                                            {
                                                constantValue = context.SemanticModel.GetConstantValue(expressionElementSyntax.Expression);
                                                if (constantValue.HasValue)
                                                {
                                                    if (constantValue.Value is string benchmarkCategoryValue)
                                                    {
                                                        benchmarkCategories.Add(benchmarkCategoryValue);
                                                    }
                                                    else if (constantValue.Value is null)
                                                    {
                                                        benchmarkCategories.Add(null);
                                                    }
                                                }
                                                else
                                                {
                                                    hasBenchmarkCategoryCompilerDiagnostics = true;

                                                    break;
                                                }
                                            }
                                        }

                                        continue;
                                    }

                                    // Array creation expression

                                    var attributeArgumentSyntaxValueType = context.SemanticModel.GetTypeInfo(attributeSyntax.ArgumentList.Arguments[0].Expression).Type;
                                    if (attributeArgumentSyntaxValueType is IArrayTypeSymbol arrayTypeSymbol)
                                    {
                                        if (arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_String)
                                        {
                                            if (attributeSyntax.ArgumentList.Arguments[0].Expression is ArrayCreationExpressionSyntax arrayCreationExpressionSyntax)
                                            {
                                                if (arrayCreationExpressionSyntax.Initializer != null)
                                                {
                                                    foreach (var expressionSyntax in arrayCreationExpressionSyntax.Initializer.Expressions)
                                                    {
                                                        constantValue = context.SemanticModel.GetConstantValue(expressionSyntax);
                                                        if (constantValue.HasValue)
                                                        {
                                                            if (constantValue.Value is string benchmarkCategoryValue)
                                                            {
                                                                benchmarkCategories.Add(benchmarkCategoryValue);
                                                            }
                                                            else if (constantValue.Value is null)
                                                            {
                                                                benchmarkCategories.Add(null);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            hasBenchmarkCategoryCompilerDiagnostics = true;

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        continue;
                                    }

                                    // Params value

                                    constantValue = context.SemanticModel.GetConstantValue(attributeSyntax.ArgumentList.Arguments[0].Expression);
                                    if (constantValue.HasValue)
                                    {
                                        if (constantValue.Value is null)
                                        {
                                            hasBenchmarkCategoryCompilerDiagnostics = true;
                                        }
                                        else if (constantValue.Value is string benchmarkCategoryValue)
                                        {
                                            benchmarkCategories.Add(benchmarkCategoryValue);
                                        }
                                    }
                                }
                                else if (attributeSyntax.ArgumentList is { Arguments.Count: > 1 })
                                {
                                    // Params values

                                    foreach (var parameterValueAttributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
                                    {
                                        var constantValue = context.SemanticModel.GetConstantValue(parameterValueAttributeArgumentSyntax.Expression);
                                        if (constantValue.HasValue)
                                        {
                                            if (constantValue.Value is string benchmarkCategoryValue)
                                            {
                                                benchmarkCategories.Add(benchmarkCategoryValue);
                                            }
                                            else if (constantValue.Value is null)
                                            {
                                                benchmarkCategories.Add(null);
                                            }
                                        }
                                        else
                                        {
                                            hasBenchmarkCategoryCompilerDiagnostics = true;

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (benchmarkAttributeUsages.Count == 1)
                    {
                        hasBenchmarkMethods = true;

                        if (!methodDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(MethodMustBePublicRule, methodDeclarationSyntax.Identifier.GetLocation(), methodDeclarationSyntax.Identifier.ToString()));
                        }

                        if (methodDeclarationSyntax.TypeParameterList != null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(MethodMustBeNonGenericRule, methodDeclarationSyntax.TypeParameterList.GetLocation(), methodDeclarationSyntax.Identifier.ToString()));
                        }

                        if (!hasBenchmarkCategoryCompilerDiagnostics)
                        {
                            if (benchmarkCategories.Count > 0 && benchmarkAttributeUsages[0].ArgumentList != null)
                            {
                                foreach (var attributeArgumentSyntax in benchmarkAttributeUsages[0].ArgumentList.Arguments)
                                {
                                    if (attributeArgumentSyntax.NameEquals != null && attributeArgumentSyntax.NameEquals.Name.Identifier.ValueText == "Baseline")
                                    {
                                        var constantValue = context.SemanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
                                        if (constantValue is { HasValue: true, Value: true })
                                        {
                                            var benchmarkCategoryFormatted = FormatBenchmarkCategory(benchmarkCategories);
                                            var baselineLocation = attributeArgumentSyntax.GetLocation();

                                            if (benchmarkCategoryBenchmarkAttributeBaselineLocations.TryGetValue(benchmarkCategoryFormatted, out var baselineLocationsPerUniqueBenchmarkCategory))
                                            {
                                                baselineLocationsPerUniqueBenchmarkCategory.Add(baselineLocation);
                                            }
                                            else
                                            {
                                                benchmarkCategoryBenchmarkAttributeBaselineLocations[benchmarkCategoryFormatted] = [ baselineLocation ];
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (benchmarkAttributeUsages[0].ArgumentList != null)
                                {
                                    foreach (var attributeArgumentSyntax in benchmarkAttributeUsages[0].ArgumentList.Arguments)
                                    {
                                        if (attributeArgumentSyntax.NameEquals != null && attributeArgumentSyntax.NameEquals.Name.Identifier.ValueText == "Baseline")
                                        {
                                            var constantValue = context.SemanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
                                            if (constantValue is { HasValue: true, Value: true })
                                            {
                                                nullBenchmarkCategoryBenchmarkAttributeBaselineLocations.Add(attributeArgumentSyntax.GetLocation());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (hasBenchmarkMethods)
            {
                if (classStaticModifier.HasValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ClassMustBeNonStaticRule, classStaticModifier.Value.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
                }

                if (nullBenchmarkCategoryBenchmarkAttributeBaselineLocations.Count >= 2)
                {
                    foreach (var baselineLocation in nullBenchmarkCategoryBenchmarkAttributeBaselineLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(OnlyOneMethodCanBeBaselineRule, baselineLocation));
                    }
                }

                var singularBenchmarkCategoryBenchmarkAttributeBaselineLocations = new Dictionary<string, List<Location>>(benchmarkCategoryBenchmarkAttributeBaselineLocations);

                foreach (var (benchmarkCategory, baselineLocations) in benchmarkCategoryBenchmarkAttributeBaselineLocations)
                {
                    if (baselineLocations.Count > 1)
                    {
                        foreach (var baselineLocation in baselineLocations)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(OnlyOneMethodCanBeBaselinePerCategoryRule, baselineLocation));
                        }

                        singularBenchmarkCategoryBenchmarkAttributeBaselineLocations.Remove(benchmarkCategory);
                    }
                }

                if (nullBenchmarkCategoryBenchmarkAttributeBaselineLocations.Count == 1 || singularBenchmarkCategoryBenchmarkAttributeBaselineLocations.Count > 0)
                {
                    var hasDuplicateBaselineBenchmarkMethodNullCategories = false;
                    var duplicateBaselineBenchmarkMethodCategories = new HashSet<string>();

                    var benchmarkClassTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                    if (benchmarkClassTypeSymbol is { TypeKind: TypeKind.Class })
                    {
                        var baseType = benchmarkClassTypeSymbol.OriginalDefinition.BaseType;

                        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                        {
                            foreach (var member in baseType.GetMembers())
                            {
                                var hasBenchmarkCategoryCompilerDiagnostics = false;
                                var benchmarkCategories = new List<string?>();
                                var benchmarkAttributeUsages = new List<AttributeData>();

                                if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol)
                                {
                                    var methodAttributes = methodSymbol.GetAttributes();

                                    foreach (var attribute in methodAttributes)
                                    {
                                        if (attribute.AttributeClass.Equals(benchmarkAttributeTypeSymbol, SymbolEqualityComparer.Default))
                                        {
                                            benchmarkAttributeUsages.Add(attribute);
                                        }
                                        else if (attribute.AttributeClass.Equals(benchmarkCategoryAttributeTypeSymbol, SymbolEqualityComparer.Default))
                                        {
                                            foreach (var benchmarkCategoriesArray in attribute.ConstructorArguments)
                                            {
                                                if (!benchmarkCategoriesArray.IsNull)
                                                {
                                                    foreach (var benchmarkCategory in benchmarkCategoriesArray.Values)
                                                    {
                                                        if (benchmarkCategory.Kind == TypedConstantKind.Primitive)
                                                        {
                                                            if (benchmarkCategory.Value == null)
                                                            {
                                                                benchmarkCategories.Add(null);
                                                            }
                                                            else if (benchmarkCategory.Value is string benchmarkCategoryValue)
                                                            {
                                                                benchmarkCategories.Add(benchmarkCategoryValue);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            hasBenchmarkCategoryCompilerDiagnostics = true;

                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (benchmarkAttributeUsages.Count == 1)
                                {
                                    if (!hasBenchmarkCategoryCompilerDiagnostics)
                                    {
                                        if (benchmarkCategories.Count > 0)
                                        {
                                            var benchmarkCategoryFormatted = FormatBenchmarkCategory(benchmarkCategories);
                                            if (singularBenchmarkCategoryBenchmarkAttributeBaselineLocations.ContainsKey(benchmarkCategoryFormatted))
                                            {
                                                if (!duplicateBaselineBenchmarkMethodCategories.Contains(benchmarkCategoryFormatted))
                                                {
                                                    if (benchmarkAttributeUsages[0].NamedArguments.Any(na => na is { Key: "Baseline", Value.Value: true }))
                                                    {
                                                        duplicateBaselineBenchmarkMethodCategories.Add(benchmarkCategoryFormatted);
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (    nullBenchmarkCategoryBenchmarkAttributeBaselineLocations.Count == 1
                                                && !hasDuplicateBaselineBenchmarkMethodNullCategories
                                                &&  benchmarkAttributeUsages[0].NamedArguments.Any(na => na is { Key: "Baseline", Value.Value: true }))
                                            {
                                                hasDuplicateBaselineBenchmarkMethodNullCategories = true;
                                            }
                                        }
                                    }
                                }
                            }

                            baseType = baseType.OriginalDefinition.BaseType;
                        }
                    }

                    if (nullBenchmarkCategoryBenchmarkAttributeBaselineLocations.Count == 1  && hasDuplicateBaselineBenchmarkMethodNullCategories)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(OnlyOneMethodCanBeBaselineRule, nullBenchmarkCategoryBenchmarkAttributeBaselineLocations[0]));
                    }

                    foreach (var duplicateBaselineBenchmarkMethodCategory in duplicateBaselineBenchmarkMethodCategories)
                    {
                        if (singularBenchmarkCategoryBenchmarkAttributeBaselineLocations.TryGetValue(duplicateBaselineBenchmarkMethodCategory, out var baselineLocations))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(OnlyOneMethodCanBeBaselinePerCategoryRule, baselineLocations[0]));
                        }
                    }
                }
            }
        }

        private static void AnalyzeAttributeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeSyntax attributeSyntax)
            {
                return;
            }

            var benchmarkCategoryAttributeTypeSymbol = GetBenchmarkCategoryTypeSymbol(context.Compilation);
            if (benchmarkCategoryAttributeTypeSymbol == null)
            {
                return;
            }

            var attributeTypeSymbol = context.SemanticModel.GetTypeInfo(attributeSyntax).Type;
            if (attributeTypeSymbol != null && attributeTypeSymbol.Equals(benchmarkCategoryAttributeTypeSymbol, SymbolEqualityComparer.Default))
            {
                if (attributeSyntax.ArgumentList is { Arguments.Count: 1 })
                {
                    var argumentSyntax = attributeSyntax.ArgumentList.Arguments[0];

                    var constantValue = context.SemanticModel.GetConstantValue(argumentSyntax.Expression);
                    if (constantValue is { HasValue: true, Value: null })
                    {
                        context.ReportDiagnostic(Diagnostic.Create(SingleNullArgumentToBenchmarkCategoryAttributeNotAllowedRule, argumentSyntax.GetLocation()));
                    }
                }
            }
        }

        private static INamedTypeSymbol? GetBenchmarkCategoryTypeSymbol(Compilation compilation) => compilation.GetTypeByMetadataName("BenchmarkDotNet.Attributes.BenchmarkCategoryAttribute");

        private static string FormatBenchmarkCategory(List<string?> benchmarkCategories)
        {
            // Default ICategoryDiscoverer implementation: DefaultCategoryDiscoverer

            return string.Join(",", benchmarkCategories.Distinct(StringComparer.OrdinalIgnoreCase));
        }
    }
}
