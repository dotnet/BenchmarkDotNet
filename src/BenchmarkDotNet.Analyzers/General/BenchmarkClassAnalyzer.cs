namespace BenchmarkDotNet.Analyzers.General
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    using System.Collections.Immutable;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BenchmarkClassAnalyzer : DiagnosticAnalyzer
    {
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

        internal static readonly DiagnosticDescriptor ClassMustBePublicRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassMustBePublic,
                                                                                                       AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBePublic_Title)),
                                                                                                       AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBePublic_MessageFormat)),
                                                                                                       "Usage",
                                                                                                       DiagnosticSeverity.Error,
                                                                                                       isEnabledByDefault: true,
                                                                                                       description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBePublic_Description)));

        internal static readonly DiagnosticDescriptor ClassMustBeNonStaticRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassMustBeNonStatic,
                                                                                                          AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_Title)),
                                                                                                          AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_MessageFormat)),
                                                                                                          "Usage",
                                                                                                          DiagnosticSeverity.Error,
                                                                                                          isEnabledByDefault: true,
                                                                                                          description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassMustBeNonStatic_Description)));

        internal static readonly DiagnosticDescriptor ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract,
                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_Title)),
                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_MessageFormat)),
                                                                                                                                             "Usage",
                                                                                                                                             DiagnosticSeverity.Error,
                                                                                                                                             isEnabledByDefault: true,
                                                                                                                                             description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ClassWithGenericTypeArgumentsAttributeMustBeNonAbstract_Description)));

        internal static readonly DiagnosticDescriptor GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttributeRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttribute,
                                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttribute_Title)),
                                                                                                                                                             AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttribute_MessageFormat)),
                                                                                                                                                             "Usage",
                                                                                                                                                             DiagnosticSeverity.Error,
                                                                                                                                                             isEnabledByDefault: true);

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

        internal static readonly DiagnosticDescriptor OnlyOneMethodCanBeBaselineRule = new DiagnosticDescriptor(DiagnosticIds.General_BenchmarkClass_OnlyOneMethodCanBeBaseline,
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaseline_Title)),
                                                                                                                AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_OnlyOneMethodCanBeBaseline_MessageFormat)),
                                                                                                                "Usage",
                                                                                                                DiagnosticSeverity.Error,
                                                                                                                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            MethodMustBePublicRule,
            MethodMustBeNonGenericRule,
            ClassMustBePublicRule,
            ClassMustBeNonStaticRule,
            ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule,
            GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttributeRule,
            ClassWithGenericTypeArgumentsAttributeMustBeGenericRule,
            GenericTypeArgumentsAttributeMustHaveMatchingTypeParameterCountRule,
            OnlyOneMethodCanBeBaselineRule
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

                ctx.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            var genericTypeArgumentsAttributes = AnalyzerHelper.GetAttributes("BenchmarkDotNet.Attributes.GenericTypeArgumentsAttribute", context.Compilation, classDeclarationSyntax.AttributeLists, context.SemanticModel);
            if (genericTypeArgumentsAttributes.Length > 0 && classDeclarationSyntax.TypeParameterList == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ClassWithGenericTypeArgumentsAttributeMustBeGenericRule, classDeclarationSyntax.Identifier.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
            }

            var benchmarkAttributeSymbol = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(context.Compilation);
            if (benchmarkAttributeSymbol == null)
            {
                return;
            }

            var benchmarkMethodsBuilder = ImmutableArray.CreateBuilder<(MethodDeclarationSyntax Method, ImmutableArray<Location> BaselineLocations)>();

            foreach (var memberDeclarationSyntax in classDeclarationSyntax.Members)
            {
                if (memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
                {
                    var benchmarkAttributes = AnalyzerHelper.GetAttributes(benchmarkAttributeSymbol, methodDeclarationSyntax.AttributeLists, context.SemanticModel);
                    if (benchmarkAttributes.Length > 0)
                    {
                        benchmarkMethodsBuilder.Add((methodDeclarationSyntax, benchmarkAttributes.SelectMany(a => GetBaselineLocations(a)).ToImmutableArray()));
                    }
                }
            }

            var benchmarkMethods = benchmarkMethodsBuilder.ToImmutable();
            if (benchmarkMethods.Length == 0)
            {
                return;
            }

            var classIsPublic = false;
            var classStaticModifier = null as SyntaxToken?;
            var classAbstractModifier = null as SyntaxToken?;

            foreach (var modifier in classDeclarationSyntax.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.PublicKeyword))
                {
                    classIsPublic = true;
                }
                else if (modifier.IsKind(SyntaxKind.StaticKeyword))
                {
                    classStaticModifier = modifier;
                }
                else if (modifier.IsKind(SyntaxKind.AbstractKeyword))
                {
                    classAbstractModifier = modifier;
                }
            }

            if (genericTypeArgumentsAttributes.Length == 0)
            {
                if (classDeclarationSyntax.TypeParameterList != null && !classAbstractModifier.HasValue)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GenericClassMustBeAbstractOrAnnotatedWithAGenericTypeArgumentsAttributeRule, classDeclarationSyntax.TypeParameterList.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
                }
            }
            else
            {
                if (classDeclarationSyntax.TypeParameterList is { Parameters.Count: > 0 })
                {
                    foreach (var genericTypeArgumentsAttribute in genericTypeArgumentsAttributes)
                    {
                        if (genericTypeArgumentsAttribute.ArgumentList is { Arguments.Count: > 0 })
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
            }

            if (!classIsPublic)
            {
                context.ReportDiagnostic(Diagnostic.Create(ClassMustBePublicRule, classDeclarationSyntax.Identifier.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
            }

            if (classAbstractModifier.HasValue && genericTypeArgumentsAttributes.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(ClassWithGenericTypeArgumentsAttributeMustBeNonAbstractRule, classAbstractModifier.Value.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
            }

            if (classStaticModifier.HasValue)
            {
                context.ReportDiagnostic(Diagnostic.Create(ClassMustBeNonStaticRule, classStaticModifier.Value.GetLocation(), classDeclarationSyntax.Identifier.ToString()));
            }

            var baselineCount = 0;
            foreach (var benchmarkMethod in benchmarkMethods)
            {
                var methodIsPublic = benchmarkMethod.Method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
                if (!methodIsPublic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MethodMustBePublicRule, benchmarkMethod.Method.Identifier.GetLocation(), benchmarkMethod.Method.Identifier.ToString()));
                }

                if (benchmarkMethod.Method.TypeParameterList != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(MethodMustBeNonGenericRule, benchmarkMethod.Method.TypeParameterList.GetLocation(), benchmarkMethod.Method.Identifier.ToString()));
                }

                baselineCount += benchmarkMethod.BaselineLocations.Length;
            }

            if (baselineCount > 1)
            {
                foreach (var benchmarkMethod in benchmarkMethods)
                {
                    foreach (var baselineLocation in benchmarkMethod.BaselineLocations)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(OnlyOneMethodCanBeBaselineRule, baselineLocation));
                    }
                }
            }

            return;

            ImmutableArray<Location> GetBaselineLocations(AttributeSyntax attributeSyntax)
            {
                var baselineLocationsBuilder = ImmutableArray.CreateBuilder<Location>();

                if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Count == 0)
                {
                    return ImmutableArray<Location>.Empty;
                }

                foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
                {
                    if (attributeArgumentSyntax.NameEquals != null && attributeArgumentSyntax.NameEquals.Name.Identifier.ValueText == "Baseline" && attributeArgumentSyntax.Expression.IsKind(SyntaxKind.TrueLiteralExpression))
                    {
                        baselineLocationsBuilder.Add(attributeArgumentSyntax.GetLocation());
                    }
                }

                return baselineLocationsBuilder.ToImmutable();
            }
        }
    }
}
