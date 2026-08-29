using BenchmarkDotNet.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace BenchmarkDotNet.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RuntimeAndToolchainCodeFixProvider)), Shared]
public class RuntimeAndToolchainCodeFixProvider : CodeFixProvider
{
    // Mirrors the constants on BenchmarkDotNet.Analyzers.General.RuntimeAndToolchainAnalyzer (that type isn't referenced here).
    private const string KindPropertyKey = "Kind";
    private const string KindChain = "Chain";
    private const string KindStatement = "Statement";
    private const string KindInitializer = "Initializer";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.General_Job_RuntimeAndToolchainBothSet);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var kind = diagnostic.Properties.TryGetValue(KindPropertyKey, out var value) ? value : null;

        // Decided here rather than inside the fix: an offered action that changes nothing - or throws - is worse than
        // no action, and FixAll would abort on it. An initializer element is always removable.
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (kind == KindChain)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation == null || !IsReducedExtensionCall(invocation, semanticModel, context.CancellationToken))
                return;
        }
        else if (kind == KindStatement && !IsRemovableStatement(node.FirstAncestorOrSelf<ExpressionStatementSyntax>()))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove the Runtime assignment (keep the Toolchain)",
                createChangedDocument: c => RemoveRuntimeAssignmentAsync(context.Document, diagnostic.Location.SourceSpan, kind, c),
                equivalenceKey: nameof(RuntimeAndToolchainCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> RemoveRuntimeAssignmentAsync(Document document, Microsoft.CodeAnalysis.Text.TextSpan diagnosticSpan, string? kind, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        SyntaxNode? newRoot = kind switch
        {
            KindChain => RemoveChainLink(root, node),
            KindStatement => RemoveStatement(root, node),
            KindInitializer => RemoveInitializerElement(root, node),
            _ => null,
        };

        return newRoot == null ? document : document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Whether the statement can be deleted outright. An embedded statement - a brace-less <c>if</c> body, a labeled
    /// statement - cannot: it would leave the enclosing construct without one, and the syntax remover throws rather
    /// than produce that. Removing it would mean synthesizing a replacement, so no fix is offered.
    /// </summary>
    private static bool IsRemovableStatement(ExpressionStatementSyntax? statement)
        => statement?.Parent is BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax;

    /// <summary>
    /// Whether the call is the instance form <c>job.WithRuntime(r)</c> rather than the static form
    /// <c>JobExtensions.WithRuntime(job, r)</c>, where the receiver is the type and dropping the call would
    /// silently discard the job argument and produce code that does not compile.
    /// </summary>
    /// <remarks>
    /// A conditional-access chain (<c>job?.WithRuntime(r)</c>) is declined for the same reason: its receiver is a
    /// <c>MemberBindingExpressionSyntax</c>, not part of the invocation, so the link cannot be dropped by replacing
    /// the call with it.
    /// </remarks>
    private static bool IsReducedExtensionCall(InvocationExpressionSyntax invocation, SemanticModel? semanticModel, CancellationToken cancellationToken)
        => invocation.Expression is MemberAccessExpressionSyntax
        && semanticModel?.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { ReducedFrom: not null };

    // `a.WithRuntime(r).WithToolchain(t)` / `a.WithToolchain(t).WithRuntime(r)` -> drops the `.WithRuntime(r)` link
    // by replacing the WithRuntime invocation with its receiver expression.
    private static SyntaxNode? RemoveChainLink(SyntaxNode root, SyntaxNode node)
    {
        var invocation = node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        return root.ReplaceNode(invocation, memberAccess.Expression.WithTriviaFrom(invocation));
    }

    // `job.Infrastructure.Runtime = r;` -> removes the whole statement.
    private static SyntaxNode? RemoveStatement(SyntaxNode root, SyntaxNode node)
    {
        var statement = node.FirstAncestorOrSelf<ExpressionStatementSyntax>();
        if (!IsRemovableStatement(statement))
            return null;

        // Under top-level statements the assignment is wrapped in a GlobalStatement; removing the inner statement
        // would leave the wrapper empty, which the syntax remover refuses.
        return RemoveKeepingComments(root, statement!.Parent is GlobalStatementSyntax global ? global : statement);
    }

    /// <summary>
    /// Removes a node, keeping any comment written above it and any directive that would otherwise be orphaned.
    /// </summary>
    /// <remarks>
    /// A node's leading trivia ends with the indentation of its line, which would otherwise be prepended to whatever
    /// follows on top of that line's own indentation - hence the trim, and the annotation to find the trimmed node
    /// again. A comment on the SAME line is not kept: it annotates the node being removed.
    /// </remarks>
    private static SyntaxNode RemoveKeepingComments(SyntaxNode root, SyntaxNode node)
    {
        var annotation = new SyntaxAnnotation();
        var trimmed = node
            .WithLeadingTrivia(TrimTrailingWhitespace(node.GetLeadingTrivia()))
            .WithAdditionalAnnotations(annotation);

        var withTrimmed = root.ReplaceNode(node, trimmed);
        var target = withTrimmed.GetAnnotatedNodes(annotation).First();

        // KeepLeadingTrivia only covers the leading trivia of the node's FIRST token. A directive in the leading
        // trivia of an interior token is inside the removed span, so without KeepUnbalancedDirectives its partner
        // outside the node is orphaned and the result does not compile.
        return withTrimmed.RemoveNode(target, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.KeepUnbalancedDirectives)!;
    }

    private static SyntaxTriviaList TrimTrailingWhitespace(SyntaxTriviaList trivia)
    {
        int end = trivia.Count;
        while (end > 0 && trivia[end - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            end--;

        return end == trivia.Count ? trivia : SyntaxFactory.TriviaList(trivia.Take(end));
    }

    // `new InfrastructureMode { Runtime = r, Toolchain = t }` -> removes the `Runtime = r` element (and its separator).
    private static SyntaxNode? RemoveInitializerElement(SyntaxNode root, SyntaxNode node)
    {
        var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
        return assignment == null ? null : RemoveKeepingComments(root, assignment);
    }
}
