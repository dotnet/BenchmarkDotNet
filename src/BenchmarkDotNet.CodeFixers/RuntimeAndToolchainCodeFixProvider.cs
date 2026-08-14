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
        return statement == null ? null : root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
    }

    // `new InfrastructureMode { Runtime = r, Toolchain = t }` -> removes the `Runtime = r` element (and its separator).
    private static SyntaxNode? RemoveInitializerElement(SyntaxNode root, SyntaxNode node)
    {
        var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
        return assignment == null ? null : root.RemoveNode(assignment, SyntaxRemoveOptions.KeepNoTrivia);
    }
}
