using BenchmarkDotNet.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;

namespace BenchmarkDotNet.CodeFixers;

/// <summary>
/// Retypes an <c>[ArgumentsSource]</c> member from <c>object</c>/<c>object[]</c> to what the benchmark's
/// parameters actually take, which is what BDN1508 and BDN1509 suggest. Both shapes stay supported, so this is
/// the whole of the fix: nothing is reported that has to be changed.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArgumentsSourceElementTypeCodeFixProvider)), Shared]
public class ArgumentsSourceElementTypeCodeFixProvider : CodeFixProvider
{
    // Mirrors the key on BenchmarkDotNet.Analyzers.Attributes.ArgumentsAttributeAnalyzer (that type isn't referenced here).
    private const string ElementTypeKey = "ElementType";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_ShouldYieldValueTuple,
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_ShouldYieldParameterType,
        DiagnosticIds.Attributes_ArgumentsSourceAttribute_MustYieldWhatParametersTake);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null || semanticModel == null)
            return;

        // Decided here rather than inside the fix: an offered action that leaves the source not compiling is worse
        // than no action, and FixAll would carry it across every usage in the solution.
        if (Plan(root, semanticModel, diagnostic, context.CancellationToken) is not { } plan)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Yield {plan.ElementType} from the source",
                createChangedDocument: c => RetypeAsync(context.Document, diagnostic, c),
                equivalenceKey: nameof(ArgumentsSourceElementTypeCodeFixProvider)),
            diagnostic);
    }

    /// <summary>
    /// Every node the fix replaces, or null where any part of the rewrite cannot be made. Built once and checked
    /// before the fix is offered, because the declaration and the rows have to move together: retyping one and
    /// not the other leaves the source not compiling.
    /// </summary>
    private static (SyntaxNode Declaration, Dictionary<SyntaxNode, SyntaxNode> Replacements, string ElementType)? Plan(
        SyntaxNode root, SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        var targets = BenchmarkParameterTypes(node, semanticModel, cancellationToken);
        if (targets == null)
            return null;

        var declaration = FindSourceDeclaration(node, root, semanticModel, cancellationToken);
        var returnType = declaration == null ? null : ReturnTypeOf(declaration);
        if (declaration == null || returnType == null)
            return null;

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        var yields = Own<YieldStatementSyntax>(declaration)
            .Where(yield => yield.IsKind(SyntaxKind.YieldReturnStatement))
            .ToArray();

        // An iterator hands its rows back one at a time, and keeps the enumerable it is declared as - which of the
        // two it is decides how the values are read, so only the element type argument moves.
        if (yields.Length > 0)
        {
            var argument = ElementTypeArgument(returnType);
            var elementType = ElementTypeFor(
                diagnostic, yields.Select(yield => yield.Expression), targets, semanticModel, returnType.SpanStart);
            if (argument == null || elementType == null)
                return null;

            replacements[argument] = SyntaxFactory.ParseTypeName(elementType).WithTriviaFrom(argument);

            foreach (var yield in yields)
            {
                var replacement = Rewrite(yield.Expression, targets, semanticModel);
                if (replacement == null)
                    return null;

                var rewrittenYield = yield.WithExpression(replacement);

                // Where the value now opens on its own line, the space that separated it from `return` would be
                // left at the end of the line above.
                if (replacement.GetLeadingTrivia().FirstOrDefault().IsKind(SyntaxKind.EndOfLineTrivia))
                    rewrittenYield = rewrittenYield.WithReturnOrBreakKeyword(rewrittenYield.ReturnOrBreakKeyword.WithTrailingTrivia());

                replacements[yield] = rewrittenYield;
            }

            return (declaration, replacements, elementType);
        }

        // Any other source hands back one collection holding every row. Its collection type cannot survive the
        // element type changing under it - a List<object[]> does not become a List of tuples by rewriting the
        // rows - so the declaration becomes the enumerable BenchmarkDotNet reads it as either way.
        var collections = RowCollections(declaration);
        if (collections.Count == 0)
            return null;

        var elementTypeOfRows = ElementTypeFor(
            diagnostic, collections.SelectMany(collection => Elements(collection) ?? []), targets, semanticModel, returnType.SpanStart);

        if (elementTypeOfRows == null)
            return null;

        foreach (var collection in collections)
        {
            var rows = Elements(collection);
            if (rows == null)
                return null;

            var rewrittenRows = new Dictionary<SyntaxNode, SyntaxNode>();

            foreach (var row in rows)
            {
                var replacement = Rewrite(row, targets, semanticModel);
                if (replacement == null)
                    return null;

                rewrittenRows[row] = replacement;
            }

            // The rows are replaced where they stand, so the brackets, the commas and everything written between
            // them come through untouched - only the collection's own syntax is rebuilt around them.
            var replacementCollection = Collection(collection.ReplaceNodes(rewrittenRows.Keys, (original, _) => rewrittenRows[original]));
            if (replacementCollection == null)
                return null;

            replacements[collection] = replacementCollection;
        }

        replacements[returnType] = EnumerableOf(elementTypeOfRows, semanticModel, returnType.SpanStart).WithTriviaFrom(returnType);

        return (declaration, replacements, elementTypeOfRows);
    }

    /// <summary>
    /// The type the source is retyped to yield: the one the diagnostic named, or - where it could name none - the
    /// type the rows turn out to hold. A by-ref-like parameter is the case with no name to give: no source can
    /// yield a ref struct, so what it should yield is whatever converts to it, which only the rows say. They have
    /// to agree on one type, which is then written as it binds where the declaration is, rather than in full.
    /// </summary>
    private static string? ElementTypeFor(
        Diagnostic diagnostic, IEnumerable<ExpressionSyntax?> rows, IReadOnlyList<ITypeSymbol> targets,
        SemanticModel semanticModel, int position)
    {
        // The diagnostic names the type in full, which reads well in a message and badly in code. Bound where
        // the declaration is, it can be written the way someone would write it there.
        if (diagnostic.Properties.TryGetValue(ElementTypeKey, out var named) && !string.IsNullOrEmpty(named))
            return AsWrittenAt(named!, semanticModel, position);

        if (targets.Count != 1)
            return null;

        string? held = null;

        foreach (var row in rows)
        {
            if (Items(row) is not { } list || list.Expressions.Length != 1)
                return null;

            var value = list.Expressions[0];
            var type = semanticModel.GetTypeInfo(value).Type;

            if (type == null || type.TypeKind == TypeKind.Error)
                return null;

            string name = type.ToMinimalDisplayString(semanticModel, position);

            if (held != null && held != name)
                return null;

            held = name;
        }

        return held;
    }

    /// <summary>
    /// What one row becomes: the value itself where a single parameter takes it, otherwise a tuple built from the
    /// arguments the row holds today. Null where the values cannot be carried over unchanged - an expression that
    /// only converts because it is on its way into an <c>object[]</c> does not convert once the tuple names the
    /// parameter's own type.
    /// </summary>
    private static ExpressionSyntax? Rewrite(ExpressionSyntax? expression, IReadOnlyList<ITypeSymbol> targets, SemanticModel semanticModel)
    {
        if (expression == null)
            return null;

        if (targets.Count == 1)
            return Converting(expression, targets[0], semanticModel) ?? Unwrapped(expression, targets[0], semanticModel);

        var items = Items(expression);
        if (items == null || items.Value.Expressions.Length != targets.Count)
            return null;

        var list = items.Value;
        var rewritten = new List<SyntaxNodeOrToken>(list.Parts.Length);
        int index = 0;

        foreach (var part in list.Parts)
        {
            if (!part.IsNode)
            {
                // The separator verbatim, so a comment or a line break written after a value stays where it was.
                rewritten.Add(part);
                continue;
            }

            var argument = Converting(list.Expressions[index++], targets[index - 1], semanticModel);
            if (argument == null)
                return null;

            rewritten.Add(SyntaxFactory.Argument(argument));
        }

        // Only the syntax around the values is rebuilt, so the row's own layout comes with them. Each
        // parenthesis stands where its bracket did, on the same line and at the same indent.
        bool spansLines = SpansLines(expression);

        var open = SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithLeadingTrivia(OpenLeading(expression, list.Open, spansLines));
        var close = SyntaxFactory.Token(SyntaxKind.CloseParenToken).WithTrailingTrivia(expression.GetTrailingTrivia());

        if (spansLines)
        {
            open = open.WithTrailingTrivia(list.Open.TrailingTrivia);
            close = close.WithLeadingTrivia(list.Close.LeadingTrivia);
        }
        else if (rewritten.Count > 0)
        {
            // On one line the brackets' own spacing is all there is, and carrying it reads as `( 1, "one" )`.
            rewritten[0] = WithoutOuterWhitespace(rewritten[0], leading: true);
            rewritten[rewritten.Count - 1] = WithoutOuterWhitespace(rewritten[rewritten.Count - 1], leading: false);
        }

        return SyntaxFactory.TupleExpression(open, SyntaxFactory.SeparatedList<ArgumentSyntax>(rewritten), close);
    }

    /// <summary>
    /// A single argument is fed the value itself, so a row that wraps one in a one-element array is that value -
    /// which is what makes the refused shape mechanical to fix. Null where the row holds anything else, or where
    /// something is written on the array syntax that unwrapping it would take with it.
    /// </summary>
    private static ExpressionSyntax? Unwrapped(ExpressionSyntax expression, ITypeSymbol target, SemanticModel semanticModel)
    {
        if (Items(expression) is not { } list || list.Expressions.Length != 1)
            return null;

        var value = list.Expressions[0];

        if (expression.DescendantTrivia(descendIntoTrivia: true)
            .Any(trivia => IsAuthored(trivia) && !value.FullSpan.Contains(trivia.SpanStart)))
            return null;

        // BenchmarkDotNet writes the cast into a by-ref-like parameter itself, so an explicit operator reaches it
        // as well as an implicit one: the source has only to yield the type that operator converts from.
        return Converting(value, target, semanticModel, throughOperator: AnalyzerHelper.MayBeRefLike(target))
            ?.WithLeadingTrivia(expression.GetLeadingTrivia())
            .WithTrailingTrivia(expression.GetTrailingTrivia());
    }

    /// <summary>
    /// The expression as it has to be written to reach <paramref name="target"/>, or null where it cannot.
    /// A cast to <c>object</c> is dropped rather than carried over: it is there to satisfy the shape being
    /// replaced, and keeping it turns the fix into a compiler error.
    /// </summary>
    private static ExpressionSyntax? Converting(
        ExpressionSyntax expression, ITypeSymbol target, SemanticModel semanticModel, bool throughOperator = false)
    {
        if (Converts(expression, target, semanticModel, throughOperator))
            return expression;

        return expression is CastExpressionSyntax cast && !Authored(cast) && Converts(cast.Expression, target, semanticModel, throughOperator)
            ? cast.Expression.WithTriviaFrom(cast)
            : null;
    }

    /// <summary>
    /// What goes in front of the parenthesis that replaces the opening bracket. A collection expression opens with
    /// that bracket, so it keeps exactly what it had. An array creation has <c>new object[]</c> in front of it,
    /// which goes - so what was written around those tokens is carried here instead: the line they ended on, and
    /// any comment written against them, read from the first thing that is not spacing, since the spacing inside
    /// <c>new object[]</c> goes with the tokens it separated.
    /// </summary>
    private static SyntaxTriviaList OpenLeading(ExpressionSyntax expression, SyntaxToken open, bool spansLines)
    {
        if (open == expression.GetFirstToken())
            return open.LeadingTrivia;

        var removed = expression
            .DescendantTrivia(TextSpan.FromBounds(expression.SpanStart, open.FullSpan.Start), descendIntoTrivia: true)
            .SkipWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia));

        var leading = expression.GetLeadingTrivia().AddRange(removed);

        return spansLines ? leading.AddRange(open.LeadingTrivia) : leading;
    }

    /// <summary>
    /// A type name as it binds at <paramref name="position"/> - unqualified where a using or the enclosing type
    /// already reaches it. The name itself is returned unchanged where it does not bind to a type, which leaves
    /// whatever the diagnostic said rather than replacing it with a guess.
    /// </summary>
    private static string AsWrittenAt(string name, SemanticModel semanticModel, int position)
    {
        var type = semanticModel
            .GetSpeculativeTypeInfo(position, SyntaxFactory.ParseTypeName(name), SpeculativeBindingOption.BindAsTypeOrNamespace)
            .Type;

        return type == null || type.TypeKind == TypeKind.Error
            ? name
            : type.ToMinimalDisplayString(semanticModel, position);
    }

    private static bool SpansLines(SyntaxNode node)
        => node.DescendantTrivia(descendIntoTrivia: true).Any(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));

    // Only whitespace is dropped, so a comment written against the outermost value stays with it.
    private static SyntaxNodeOrToken WithoutOuterWhitespace(SyntaxNodeOrToken part, bool leading)
    {
        var trivia = leading ? part.GetLeadingTrivia() : part.GetTrailingTrivia();

        if (!trivia.All(one => one.IsKind(SyntaxKind.WhitespaceTrivia)))
            return part;

        return leading ? part.WithLeadingTrivia() : part.WithTrailingTrivia();
    }

    /// <summary>
    /// Whether anything is written inside this expression that rebuilding it cannot carry - a comment, or a
    /// directive whose branches would have to be rewritten separately. Such an expression is left alone: dropping
    /// what its author wrote there is not a fix, and it is not the sort of loss a review notices.
    /// </summary>
    private static bool Authored(SyntaxNode node)
        => node.DescendantTrivia(descendIntoTrivia: true).Any(IsAuthored);

    private static bool IsAuthored(SyntaxTrivia trivia)
        => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
        || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
        || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
        || trivia.IsDirective;

    private static bool Converts(ExpressionSyntax expression, ITypeSymbol target, SemanticModel semanticModel, bool throughOperator)
    {
        var conversion = semanticModel.ClassifyConversion(expression, target);

        return conversion.IsIdentity || (conversion.Exists && (conversion.IsImplicit || throughOperator));
    }

    // The diagnostic sits on the source's name inside the attribute, so the benchmark it is attached to is the
    // method that carries the attribute, and its parameters are what the source has to feed.
    private static IReadOnlyList<ITypeSymbol>? BenchmarkParameterTypes(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var benchmark = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (benchmark == null)
            return null;

        var symbol = semanticModel.GetDeclaredSymbol(benchmark, cancellationToken);

        return symbol == null || symbol.Parameters.Length == 0
            ? null
            : symbol.Parameters.Select(parameter => parameter.Type).ToArray();
    }

    // The source's name is resolved and then found in this document - a source in another file is left alone
    // rather than edited out of sight.
    private static SyntaxNode? FindSourceDeclaration(SyntaxNode node, SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var nameofArgument = node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault()?.ArgumentList.Arguments.FirstOrDefault()?.Expression
            ?? node as ExpressionSyntax;

        if (nameofArgument == null)
            return null;

        var symbolInfo = semanticModel.GetSymbolInfo(nameofArgument, cancellationToken);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        var reference = symbol?.DeclaringSyntaxReferences.FirstOrDefault();

        return reference == null || reference.SyntaxTree != root.SyntaxTree
            ? null
            : reference.GetSyntax(cancellationToken);
    }

    private static async Task<Document> RetypeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || semanticModel == null)
            return document;

        if (Plan(root, semanticModel, diagnostic, cancellationToken) is not { } plan)
            return document;

        // One pass: every replacement is computed against the original node, so retyping the declaration cannot
        // shift the rows being rewritten beside it.
        var rewritten = plan.Declaration.ReplaceNodes(plan.Replacements.Keys, (original, _) => plan.Replacements[original]);

        return document.WithSyntaxRoot(root.ReplaceNode(plan.Declaration, rewritten));
    }

    /// <summary>
    /// The one type argument of whatever enumerable the source returns. Replacing the argument rather than
    /// rebuilding the type keeps everything the author wrote around it: which enumerable it is, and how it is
    /// named - a qualified or alias-qualified name survives, where a rebuilt one would be emitted unqualified
    /// and need a using the file may not have.
    /// </summary>
    private static TypeSyntax? ElementTypeArgument(TypeSyntax returnType)
    {
        var generic = returnType as GenericNameSyntax
            ?? (returnType as QualifiedNameSyntax)?.Right as GenericNameSyntax
            ?? (returnType as AliasQualifiedNameSyntax)?.Name as GenericNameSyntax;

        return generic == null || generic.TypeArgumentList.Arguments.Count != 1
            ? null
            : generic.TypeArgumentList.Arguments[0];
    }

    /// <summary>
    /// <c>IEnumerable&lt;elementType&gt;</c>, qualified only where the name would not otherwise bind - the source
    /// being retyped may name no namespace this file imports, as an array-returning one does not.
    /// </summary>
    private static TypeSyntax EnumerableOf(string elementType, SemanticModel semanticModel, int position)
    {
        bool inScope = semanticModel.LookupNamespacesAndTypes(position, name: "IEnumerable")
            .Any(symbol => symbol is INamedTypeSymbol { Arity: 1 } enumerable
                && enumerable.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic");

        return SyntaxFactory.ParseTypeName(
            $"{(inScope ? "IEnumerable" : "global::System.Collections.Generic.IEnumerable")}<{elementType}>");
    }

    private static TypeSyntax? ReturnTypeOf(SyntaxNode declaration)
        => declaration switch
        {
            MethodDeclarationSyntax method => method.ReturnType,
            PropertyDeclarationSyntax property => property.Type,
            _ => null,
        };

    /// <summary>Every expression the source hands its rows back as - one per <c>return</c> or <c>=&gt;</c>.</summary>
    private static IReadOnlyList<ExpressionSyntax> RowCollections(SyntaxNode declaration)
    {
        var collections = new List<ExpressionSyntax>();

        foreach (var node in Own<SyntaxNode>(declaration))
        {
            switch (node)
            {
                case ArrowExpressionClauseSyntax arrow:
                    collections.Add(arrow.Expression);
                    break;

                case ReturnStatementSyntax { Expression: { } expression }:
                    collections.Add(expression);
                    break;
            }
        }

        return collections;
    }

    // The source's own nodes: one inside a nested local function or lambda belongs to a different body, whose
    // rows these are not.
    private static IEnumerable<T> Own<T>(SyntaxNode declaration) where T : SyntaxNode
        => declaration
            .DescendantNodes(node => node == declaration || node is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax))
            .OfType<T>();

    /// <summary>
    /// The items a collection syntax holds, together with the tokens that enclose them and the separators between
    /// them - everything needed to write the values back out where they were.
    /// </summary>
    private readonly struct ItemList(SyntaxToken open, SyntaxToken close, SyntaxNodeOrToken[] parts)
    {
        public SyntaxToken Open { get; } = open;

        public SyntaxToken Close { get; } = close;

        /// <summary>The values and the separators between them, in the order they are written.</summary>
        public SyntaxNodeOrToken[] Parts { get; } = parts;

        public ExpressionSyntax[] Expressions
            => Parts.Where(part => part.IsNode).Select(part => (ExpressionSyntax) part.AsNode()!).ToArray();
    }

    /// <summary>
    /// The items of whichever collection syntax is written here: <c>new object[] { … }</c>, <c>new[] { … }</c>,
    /// a sized <c>new object[2] { … }</c>, <c>new List&lt;object[]&gt; { … }</c> or a collection expression
    /// <c>[ … ]</c>. A spread element is refused, because how many items it stands for is not written down.
    /// </summary>
    private static ItemList? Items(ExpressionSyntax? expression)
        => expression switch
        {
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => Of(initializer),
            ImplicitArrayCreationExpressionSyntax implicitArray => Of(implicitArray.Initializer),
            ObjectCreationExpressionSyntax { Initializer: { } initializer }
                when initializer.IsKind(SyntaxKind.CollectionInitializerExpression) => Of(initializer),
#if CODE_ANALYSIS_4_8
            // A collection expression is C# 12, so the node itself only exists from the Roslyn 4.8 band up - and a
            // source compiled by an older one cannot contain the syntax there is nothing here to read.
            CollectionExpressionSyntax collection => collection.Elements.All(element => element is ExpressionElementSyntax)
                ? new ItemList(
                    collection.OpenBracketToken,
                    collection.CloseBracketToken,
                    collection.Elements.GetWithSeparators()
                        .Select(part => part.IsNode ? (SyntaxNodeOrToken) ((ExpressionElementSyntax) part.AsNode()!).Expression : part)
                        .ToArray())
                : null,
#endif
            _ => null,
        };

    private static ItemList Of(InitializerExpressionSyntax initializer)
        => new(initializer.OpenBraceToken, initializer.CloseBraceToken, initializer.Expressions.GetWithSeparators().ToArray());

    private static IReadOnlyList<ExpressionSyntax>? Elements(ExpressionSyntax? expression) => Items(expression)?.Expressions;

    /// <summary>
    /// The collection that now has to produce an <c>IEnumerable&lt;T&gt;</c> of the rows it already holds. A
    /// collection expression is one already; anything else keeps its initializer and becomes an implicitly typed
    /// array, which takes the new element type from the rows and so needs no name for it - but cannot take one
    /// from no rows. The initializer comes through whole, so the rows and their layout are untouched.
    /// </summary>
    private static ExpressionSyntax? Collection(ExpressionSyntax rewritten)
        => rewritten switch
        {
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => AsImplicitArray(initializer, rewritten),
            ObjectCreationExpressionSyntax { Initializer: { } initializer } => AsImplicitArray(initializer, rewritten),
            _ => rewritten,
        };

    private static ExpressionSyntax? AsImplicitArray(InitializerExpressionSyntax initializer, ExpressionSyntax original)
        => initializer.Expressions.Count == 0
            ? null
            : SyntaxFactory.ImplicitArrayCreationExpression(AsArrayInitializer(initializer)).WithTriviaFrom(original);

    // An array's initializer is a different kind of node from a collection's, though the two read the same. Only
    // the kind changes here - the braces, the separators and the rows themselves are the ones already written.
    private static InitializerExpressionSyntax AsArrayInitializer(InitializerExpressionSyntax initializer)
        => initializer.IsKind(SyntaxKind.ArrayInitializerExpression)
            ? initializer
            : SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                initializer.OpenBraceToken,
                initializer.Expressions,
                initializer.CloseBraceToken);
}
