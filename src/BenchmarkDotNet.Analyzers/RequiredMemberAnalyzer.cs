using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BenchmarkDotNet.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequiredMemberAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor RequiredMemberCannotBeSetRule = new(
        DiagnosticIds.General_BenchmarkClass_RequiredMemberCannotBeSet,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_RequiredMemberCannotBeSet_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_RequiredMemberCannotBeSet_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_RequiredMemberCannotBeSet_Description)));

    internal static readonly DiagnosticDescriptor ConstructorMustNotSetRequiredMembersRule = new(
        DiagnosticIds.General_BenchmarkClass_ConstructorMustNotSetRequiredMembers,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ConstructorMustNotSetRequiredMembers_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ConstructorMustNotSetRequiredMembers_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_BenchmarkClass_ConstructorMustNotSetRequiredMembers_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(RequiredMemberCannotBeSetRule, ConstructorMustNotSetRequiredMembersRule);

    // Attributes whose members BDN sets when constructing the benchmark (via the object initializer or the
    // cancellation-token initializer). A `required` member with any of these is satisfied at construction.
    private static readonly string[] SettableMemberAttributeNames =
    [
        "BenchmarkDotNet.Attributes.ParamsAttribute",
        "BenchmarkDotNet.Attributes.ParamsSourceAttribute",
        "BenchmarkDotNet.Attributes.ParamsAllValuesAttribute",
        "BenchmarkDotNet.Attributes.BenchmarkCancellationAttribute",
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

#if CODE_ANALYSIS_4_4
        // `required` members (and the IPropertySymbol/IFieldSymbol.IsRequired API) are C# 11 / Roslyn 4.4+.
        context.RegisterCompilationStartAction(startContext =>
        {
            var benchmarkAttribute = AnalyzerHelper.GetBenchmarkAttributeTypeSymbol(startContext.Compilation);
            if (benchmarkAttribute == null)
            {
                return;
            }

            var settableAttributes = SettableMemberAttributeNames
                .Select(startContext.Compilation.GetTypeByMetadataName)
                .Where(symbol => symbol != null)
                .ToImmutableArray();
            var setsRequiredMembersAttribute = startContext.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");

            // The runnable derives from the benchmark type, so it must set every `required` member the type declares
            // OR inherits. Each benchmark type checks its own base chain, stopping at a base that is itself a
            // benchmark type (that one reports its own members), so no state is shared between types.
            startContext.RegisterSymbolAction(symbolContext =>
            {
                var benchmarkType = (INamedTypeSymbol)symbolContext.Symbol;
                if (benchmarkType.TypeKind != TypeKind.Class || !IsBenchmarkType(benchmarkType, benchmarkAttribute))
                {
                    return;
                }

                // The generated constructor chains to this one, so C# would force it to repeat [SetsRequiredMembers]
                // (CS9039) - which suppresses required-member checking entirely and hides required members BDN
                // cannot set. Report it rather than propagating the attribute into the generated code.
                if (setsRequiredMembersAttribute != null)
                {
                    foreach (var constructor in benchmarkType.InstanceConstructors)
                    {
                        if (constructor.Parameters.Length != 0
                            || !constructor.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, setsRequiredMembersAttribute)))
                        {
                            continue;
                        }

                        var constructorLocation = constructor.Locations.FirstOrDefault(candidate => candidate.IsInSource);
                        if (constructorLocation != null)
                        {
                            symbolContext.ReportDiagnostic(Diagnostic.Create(ConstructorMustNotSetRequiredMembersRule, constructorLocation, benchmarkType.Name));
                        }
                    }
                }

                Location? inheritedLocation = null;

                for (INamedTypeSymbol? current = benchmarkType; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
                {
                    bool declaredOnBenchmarkType = SymbolEqualityComparer.Default.Equals(current, benchmarkType);

                    // A base that is itself a benchmark type reports its own (and its bases') required members at
                    // their declarations via its own walk, so stop here to avoid flagging them again. That walk only
                    // happens for a base declared in this compilation - one from a referenced assembly is never
                    // analyzed, so keep going and report its members at this type's base-type reference instead.
                    if (!declaredOnBenchmarkType
                        && IsBenchmarkType(current, benchmarkAttribute)
                        && current.DeclaringSyntaxReferences.Length > 0)
                    {
                        break;
                    }

                    foreach (var member in current.GetMembers())
                    {
                        bool isRequired = member switch
                        {
                            IPropertySymbol property => property.IsRequired,
                            IFieldSymbol field => field.IsRequired,
                            _ => false
                        };
                        if (!isRequired)
                        {
                            continue;
                        }

                        // BDN sets [Params*] members via the object initializer and an instance [BenchmarkCancellation]
                        // member via the cancellation-token initializer, so those required members are satisfied.
                        if (member.GetAttributes().Any(attribute => settableAttributes.Any(settable => AnalyzerHelper.IsOrDerivesFrom(attribute.AttributeClass, settable))))
                        {
                            continue;
                        }

                        // A member declared on the benchmark type is reported at its own declaration; one inherited
                        // from a base (source or a referenced assembly) is reported at the benchmark class's
                        // `: BaseType` reference - the base declaration doesn't know it's inherited by a benchmark,
                        // and that reference is always in the benchmark class's own source.
                        Location location = declaredOnBenchmarkType
                            ? member.Locations.FirstOrDefault(candidate => candidate.IsInSource) ?? Location.None
                            : inheritedLocation ??= GetBaseTypeReferenceLocation(benchmarkType);

                        symbolContext.ReportDiagnostic(Diagnostic.Create(RequiredMemberCannotBeSetRule, location, member.Name));
                    }
                }
            }, SymbolKind.NamedType);
        });
#endif
    }

    private static bool IsBenchmarkType(INamedTypeSymbol type, INamedTypeSymbol benchmarkAttribute)
    {
        // The [Benchmark] method may be inherited, so walk the base types too. The attribute itself may also be
        // a user's own deriving from [Benchmark], which is what the runtime resolves.
        for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
        {
            if (current.GetMembers().OfType<IMethodSymbol>()
                .Any(method => method.GetAttributes().Any(attribute => AnalyzerHelper.IsOrDerivesFrom(attribute.AttributeClass, benchmarkAttribute))))
            {
                return true;
            }
        }

        return false;
    }

    // The `: BaseType` reference in the benchmark class's declaration (its base class in the base list), where an
    // inherited required member becomes the benchmark class's problem. Matched by name to avoid a semantic-model
    // lookup (RS1030); the base class is always the first base-list entry, but a name match is robust to partials
    // and interface-only base lists. Falls back to the type's own declaration.
    private static Location GetBaseTypeReferenceLocation(INamedTypeSymbol benchmarkType)
    {
        var baseType = benchmarkType.BaseType;
        if (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            foreach (var syntaxReference in benchmarkType.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is TypeDeclarationSyntax typeDeclaration && typeDeclaration.BaseList != null)
                {
                    foreach (var baseTypeSyntax in typeDeclaration.BaseList.Types)
                    {
                        if (GetRightmostName(baseTypeSyntax.Type) == baseType.Name)
                        {
                            return baseTypeSyntax.GetLocation();
                        }
                    }
                }
            }
        }

        return benchmarkType.Locations.FirstOrDefault(candidate => candidate.IsInSource) ?? Location.None;
    }

    private static string? GetRightmostName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        QualifiedNameSyntax qualified => GetRightmostName(qualified.Right),
        AliasQualifiedNameSyntax alias => GetRightmostName(alias.Name),
        _ => null
    };
}
