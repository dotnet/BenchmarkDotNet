using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace BenchmarkDotNet.Analyzers.General;

/// <summary>
/// Flags jobs that set both a runtime and a toolchain. The two are coupled (setting the toolchain overwrites the runtime,
/// and setting the runtime clears the toolchain), so specifying both is order-dependent and misleading: whichever is
/// assigned last wins and the other is silently discarded. The fix keeps the toolchain (the source of truth for the runtime).
/// Both the fluent form (<c>.WithRuntime(...).WithToolchain(...)</c>, in any order) and explicit property assignments
/// (<c>Infrastructure.Runtime = ...</c> / <c>Infrastructure.Toolchain = ...</c>, including object initializers) are detected.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RuntimeAndToolchainAnalyzer : DiagnosticAnalyzer
{
    // Passed through Diagnostic.Properties so the code fix knows how to remove the runtime assignment.
    internal const string KindPropertyKey = "Kind";
    internal const string KindChain = "Chain";
    internal const string KindStatement = "Statement";
    internal const string KindInitializer = "Initializer";

    internal static readonly DiagnosticDescriptor RuntimeAndToolchainBothSetRule = new(
        DiagnosticIds.General_Job_RuntimeAndToolchainBothSet,
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Job_RuntimeAndToolchainBothSet_Title)),
        AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Job_RuntimeAndToolchainBothSet_MessageFormat)),
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: AnalyzerHelper.GetResourceString(nameof(BenchmarkDotNetAnalyzerResources.General_Job_RuntimeAndToolchainBothSet_Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new DiagnosticDescriptor[]
    {
        RuntimeAndToolchainBothSetRule,
    }.ToImmutableArray();

    public override void Initialize(AnalysisContext analysisContext)
    {
        analysisContext.EnableConcurrentExecution();
        analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        analysisContext.RegisterCompilationStartAction(ctx =>
        {
            // Only run if the coupled BenchmarkDotNet APIs are referenced.
            var jobExtensionsSymbol = ctx.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Jobs.JobExtensions");
            var infrastructureModeSymbol = ctx.Compilation.GetTypeByMetadataName("BenchmarkDotNet.Jobs.InfrastructureMode");
            if (jobExtensionsSymbol == null || infrastructureModeSymbol == null)
            {
                return;
            }

            ctx.RegisterOperationBlockAction(blockContext => AnalyzeOperationBlock(blockContext, jobExtensionsSymbol, infrastructureModeSymbol));
        });
    }

    private sealed class GroupInfo
    {
        // Each runtime setter that should be reported when this group also has a toolchain setter.
        public List<(Location Location, string Kind)> RuntimeSetters { get; } = [];
        public bool HasToolchainSetter { get; set; }
    }

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context, INamedTypeSymbol jobExtensionsSymbol, INamedTypeSymbol infrastructureModeSymbol)
    {
        // Group runtime/toolchain setters that operate on the same job. The key identifies the shared job:
        //  * fluent chains -> the outermost invocation node of the chain (same node instance for every link),
        //  * object initializers -> the enclosing initializer node,
        //  * sequential property assignments -> the textual receiver (scoped to this operation block).
        var groups = new Dictionary<object, GroupInfo>();

        foreach (var operationBlock in context.OperationBlocks)
        {
            foreach (var operation in operationBlock.DescendantsAndSelf())
            {
                switch (operation)
                {
                    case IInvocationOperation invocation:
                        AnalyzeInvocation(invocation, jobExtensionsSymbol, groups);
                        break;
                    case ISimpleAssignmentOperation assignment:
                        AnalyzeAssignment(assignment, infrastructureModeSymbol, groups);
                        break;
                }
            }
        }

        foreach (var group in groups.Values)
        {
            if (!group.HasToolchainSetter)
            {
                continue;
            }

            foreach (var (location, kind) in group.RuntimeSetters)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RuntimeAndToolchainBothSetRule,
                    location,
                    ImmutableDictionary.Create<string, string?>().Add(KindPropertyKey, kind)));
            }
        }
    }

    private static void AnalyzeInvocation(IInvocationOperation invocation, INamedTypeSymbol jobExtensionsSymbol, Dictionary<object, GroupInfo> groups)
    {
        var method = invocation.TargetMethod;
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, jobExtensionsSymbol))
        {
            return;
        }

        bool isRuntime = method.Name == "WithRuntime";
        bool isToolchain = method.Name == "WithToolchain";
        if (!isRuntime && !isToolchain)
        {
            return;
        }

        var group = GetOrAdd(groups, GetInvocationChainRoot(invocation.Syntax));
        if (isToolchain)
        {
            group.HasToolchainSetter = true;
        }
        else
        {
            group.RuntimeSetters.Add((GetInvocationNameLocation(invocation.Syntax), KindChain));
        }
    }

    private static void AnalyzeAssignment(ISimpleAssignmentOperation assignment, INamedTypeSymbol infrastructureModeSymbol, Dictionary<object, GroupInfo> groups)
    {
        if (assignment.Target is not IPropertyReferenceOperation propertyReference
            || !SymbolEqualityComparer.Default.Equals(propertyReference.Property.ContainingType, infrastructureModeSymbol))
        {
            return;
        }

        bool isRuntime = propertyReference.Property.Name == "Runtime";
        bool isToolchain = propertyReference.Property.Name == "Toolchain";
        if (!isRuntime && !isToolchain)
        {
            return;
        }

        object key;
        string kind;
        if (propertyReference.Instance is IInstanceReferenceOperation)
        {
            // Object/collection initializer: `new InfrastructureMode { Runtime = ..., Toolchain = ... }`
            // or `new Job { Infrastructure = { Runtime = ..., Toolchain = ... } }`.
            var initializer = assignment.Syntax.FirstAncestorOrSelf<InitializerExpressionSyntax>();
            if (initializer == null)
            {
                return;
            }
            key = initializer;
            kind = KindInitializer;
        }
        else
        {
            // Explicit receiver: `job.Infrastructure.Runtime = ...`. Group by the receiver text within this block.
            key = "prop:" + (propertyReference.Instance?.Syntax.ToString() ?? string.Empty);
            kind = KindStatement;
        }

        var group = GetOrAdd(groups, key);
        if (isToolchain)
        {
            group.HasToolchainSetter = true;
        }
        else
        {
            group.RuntimeSetters.Add((assignment.Syntax.GetLocation(), kind));
        }
    }

    private static GroupInfo GetOrAdd(Dictionary<object, GroupInfo> groups, object key)
    {
        if (!groups.TryGetValue(key, out var group))
        {
            group = new GroupInfo();
            groups[key] = group;
        }
        return group;
    }

    // Walks up a fluent chain (`a.WithX(..).WithY(..)`) and returns the outermost invocation node, which is the same
    // instance for every link, so all links in one chain share a grouping key.
    private static SyntaxNode GetInvocationChainRoot(SyntaxNode invocation)
    {
        var current = invocation;
        while (current.Parent is MemberAccessExpressionSyntax memberAccess
            && memberAccess.Expression == current
            && memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
        {
            current = parentInvocation;
        }
        return current;
    }

    // Squiggles just the `WithRuntime` name, falling back to the whole invocation if it isn't a member-access call.
    private static Location GetInvocationNameLocation(SyntaxNode invocation)
        => invocation is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess }
            ? memberAccess.Name.GetLocation()
            : invocation.GetLocation();
}
