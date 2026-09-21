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

    internal static readonly DiagnosticDescriptor MustYieldArgumentListRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_MustYieldArgumentList,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldArgumentList_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldArgumentList_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldArgumentList_Description)));

    internal static readonly DiagnosticDescriptor MustMatchParametersForEveryTypeArgumentRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_MustMatchParametersForEveryTypeArgument,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustMatchParametersForEveryTypeArgument_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustMatchParametersForEveryTypeArgument_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustMatchParametersForEveryTypeArgument_Description)));

    internal static readonly DiagnosticDescriptor MustYieldWhatParametersTakeRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_MustYieldWhatParametersTake,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldWhatParametersTake_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldWhatParametersTake_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_MustYieldWhatParametersTake_Description)));

    internal static readonly DiagnosticDescriptor ShouldYieldValueTupleRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_ShouldYieldValueTuple,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldValueTuple_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldValueTuple_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldValueTuple_Description)));

    internal static readonly DiagnosticDescriptor ShouldYieldParameterTypeRule = new(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_ShouldYieldParameterType,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldParameterType_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldParameterType_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.Attributes_ArgumentsSourceAttribute_ShouldYieldParameterType_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        RequiresBenchmarkAttributeRule,
        MustHaveMatchingValueCountRule,
        MustHaveMatchingValueTypeRule,
        RequiresParametersRule,
        ArgumentsSourceMustReturnEnumerableRule,
        MustYieldArgumentListRule,
        MustMatchParametersForEveryTypeArgumentRule,
        MustYieldWhatParametersTakeRule,
        ShouldYieldValueTupleRule,
        ShouldYieldParameterTypeRule,
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

            AnalyzeElementAgainstParameters(attr, sourceName!, elementType!);
        }

        // Mirrors SmartParamBuilder.Admits: one parameter is fed the value itself, several are fed an object[] or a
        // ValueTuple naming each of them, and every item is judged against the parameter in its position by the
        // same test - the parameter's own type, or object, or a conversion operator where it is by-ref-like.
        void AnalyzeElementAgainstParameters(AttributeData attr, string sourceName, ITypeSymbol elementType)
        {
            var parameters = methodSymbol.Parameters;
            var location = attr.GetSourceNameLocation();

            if (parameters.Length == 1)
            {
                if (IsObject(elementType))
                {
                    // object stays valid - it is the way to defer the decision to the value's runtime type - so this
                    // only suggests naming the parameter's type, and says nothing when that type is object already.
                    // Nor where the parameter is by-ref-like: no source can yield one, so object is all there is.
                    if (!IsObject(parameters[0].Type) && !AnalyzerHelper.MayBeRefLike(parameters[0].Type))
                        context.ReportDiagnostic(Diagnostic.Create(
                            ShouldYieldParameterTypeRule, location, Replacement(parameters[0].Type.ToDisplayString()),
                            sourceName, parameters[0].Type.ToDisplayString()));
                    return;
                }

                // The refused shape is the one-element array that used to wrap a single argument, and unwrapping
                // it is mechanical, so the parameter's type is carried for the fixer to retype the source to. Not
                // where the parameter is by-ref-like: no source can yield one, so there is nothing to name.
                var unwrapped = IsObjectArray(elementType) && !AnalyzerHelper.MayBeRefLike(parameters[0].Type)
                    ? Replacement(parameters[0].Type.ToDisplayString())
                    : null;

                ReportIfNotAdmissible(location, sourceName, elementType, parameters[0], string.Empty, unwrapped);
                return;
            }

            if (IsObjectArray(elementType))
            {
                // A ValueTuple cannot hold a ref struct, so where any parameter is by-ref-like there is no tuple to
                // suggest and object[] is the shape that feeds it.
                if (!parameters.Any(parameter => AnalyzerHelper.MayBeRefLike(parameter.Type)))
                    context.ReportDiagnostic(Diagnostic.Create(
                        ShouldYieldValueTupleRule, location, Replacement(TupleOf(parameters)),
                        sourceName, TupleOf(parameters)));
                return;
            }

            if (!TryGetTupleItems(elementType, parameters.Length, out var items))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MustYieldArgumentListRule, location, sourceName, elementType.ToDisplayString(),
                    parameters.Length, methodSymbol.Name));
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (ReportIfNotAdmissible(location, sourceName, items[i], parameters[i], $" for argument {i + 1}"))
                    return;
            }
        }

        // True when something was reported, so the caller stops at the first item that does not fit.
        bool ReportIfNotAdmissible(Location location, string sourceName, ITypeSymbol type, IParameterSymbol parameter, string position,
            ImmutableDictionary<string, string?>? properties = null)
        {
            // An open declaration cannot be settled here: what a type parameter becomes is supplied by the next
            // [GenericTypeArguments] on the class, which this deliberately does not read. Discovery holds the
            // substituted types and reports an error on those, so this says only that it cannot be proved.
            if (ContainsTypeParameter(type) || ContainsTypeParameter(parameter.Type))
            {
                if (!SymbolEqualityComparer.Default.Equals(type, parameter.Type))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MustMatchParametersForEveryTypeArgumentRule, location,
                        sourceName, type.ToDisplayString(), parameter.Type.ToDisplayString()));
                    return true;
                }

                return false;
            }

            if (Admissible(type, parameter.Type))
                return false;

            context.ReportDiagnostic(Diagnostic.Create(
                MustYieldWhatParametersTakeRule, location, properties,
                sourceName, type.ToDisplayString(), parameter.Type.ToDisplayString(), position));
            return true;
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


    // The element type the fixer retypes the source to, carried on the diagnostic so the fix does not have to
    // work it out a second time from a different starting point.
    internal const string ElementTypeKey = "ElementType";

    private static ImmutableDictionary<string, string?> Replacement(string elementType)
        => ImmutableDictionary<string, string?>.Empty.Add(ElementTypeKey, elementType);

    private static bool IsObject(ITypeSymbol type) => type.SpecialType == SpecialType.System_Object;

    private static bool IsObjectArray(ITypeSymbol type)
        => type is IArrayTypeSymbol { Rank: 1 } array && IsObject(array.ElementType);

    private static string TupleOf(ImmutableArray<IParameterSymbol> parameters)
        => "(" + string.Join(", ", parameters.Select(parameter => parameter.Type.ToDisplayString())) + ")";

    // Roslyn flattens the Rest chain for us, so a tuple of any length reads the same way here as it does in
    // SmartParamBuilder, which walks one Rest per seven items.
    private static bool TryGetTupleItems(ITypeSymbol elementType, int arity, out IReadOnlyList<ITypeSymbol> items)
    {
        if (elementType is INamedTypeSymbol { IsTupleType: true } tuple && tuple.TupleElements.Length == arity)
        {
            items = tuple.TupleElements.Select(element => element.Type).ToArray();
            return true;
        }

        items = [];
        return false;
    }

    // The static-analysis counterpart of SmartParamBuilder.Admissible.
    private static bool Admissible(ITypeSymbol type, ITypeSymbol parameterType)
        => SymbolEqualityComparer.Default.Equals(type, parameterType)
        || (AnalyzerHelper.MayBeRefLike(parameterType) && DeclaresConversion(parameterType, type));

    // A conversion operator written for exactly these two types, on either of them - the same question
    // ReflectionExtensions.TakesByConversion asks of the runtime types.
    private static bool DeclaresConversion(ITypeSymbol targetType, ITypeSymbol sourceType)
        // Check the string-to-ReadOnlySpan<char> special-case first.
        => IsStringToReadOnlySpanOfChar(targetType, sourceType)
        || Declares(targetType, targetType, sourceType)
        || Declares(sourceType, targetType, sourceType);

    private static bool IsStringToReadOnlySpanOfChar(ITypeSymbol targetType, ITypeSymbol sourceType)
        => sourceType.SpecialType == SpecialType.System_String
        && targetType is INamedTypeSymbol { Name: "ReadOnlySpan", Arity: 1 } span
        && span.ContainingNamespace?.ToDisplayString() == "System"
        && span.TypeArguments[0].SpecialType == SpecialType.System_Char;

    private static bool Declares(ITypeSymbol declaringType, ITypeSymbol targetType, ITypeSymbol sourceType)
        => declaringType.GetMembers()
            .OfType<IMethodSymbol>()
            .Any(method => method.MethodKind == MethodKind.Conversion
                && SymbolEqualityComparer.Default.Equals(method.ReturnType, targetType)
                && method.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, sourceType));

    // Whether anything in the type is still a type parameter, which is what makes the match unprovable here.
    private static bool ContainsTypeParameter(ITypeSymbol type)
        => type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
            _ => false,
        };
}