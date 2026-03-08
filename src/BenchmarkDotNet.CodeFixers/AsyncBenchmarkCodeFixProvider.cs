using BenchmarkDotNet.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncBenchmarkCodeFixProvider)), Shared]
public class AsyncBenchmarkCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.General_AsyncBenchmark_ShouldHaveCancellationToken);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration identified by the diagnostic
        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
        if (methodDeclaration == null)
            return;

        // Find the containing class
        var classDeclaration = methodDeclaration.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration == null)
            return;

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [BenchmarkCancellation] CancellationToken property",
                createChangedDocument: c => AddCancellationTokenPropertyAsync(context.Document, classDeclaration, c),
                equivalenceKey: nameof(AsyncBenchmarkCodeFixProvider)),
            diagnostic);
    }

    private static async Task<Document> AddCancellationTokenPropertyAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Create the [BenchmarkCancellation] attribute
        var benchmarkCancellationAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("BenchmarkCancellation"));

        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(benchmarkCancellationAttribute));

        // Create the CancellationToken property
        var cancellationTokenProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.IdentifierName("CancellationToken"),
                SyntaxFactory.Identifier("CancellationToken"))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
            .WithAttributeLists(SyntaxFactory.SingletonList(attributeList))
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Find the best location to insert the property (after fields/properties, before methods)
        var insertionIndex = 0;
        var members = classDeclaration.Members;

        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (members[i] is FieldDeclarationSyntax or PropertyDeclarationSyntax)
            {
                insertionIndex = i + 1;
                break;
            }
        }

        // Determine the appropriate leading trivia for the new property
        var propertyToInsert = cancellationTokenProperty;
        var membersToUpdate = members;

        if (insertionIndex > 0)
        {
            // Inserting after an existing member - add blank line by replacing
            // the TRAILING trivia of the previous member with exactly two newlines
            var previousMember = members[insertionIndex - 1];
            var previousMemberWithBlankLine = previousMember.WithTrailingTrivia(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.CarriageReturnLineFeed));

            membersToUpdate = members.Replace(previousMember, previousMemberWithBlankLine);
        }

        if (insertionIndex < members.Count)
        {
            // Copy only whitespace trivia (indentation) from the next member, not newlines
            var nextMember = membersToUpdate[insertionIndex];
            var indentationTrivia = nextMember.GetLeadingTrivia()
                .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                .ToList();

            propertyToInsert = cancellationTokenProperty
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(indentationTrivia);
        }

        // Insert the new property
        var newMembers = membersToUpdate.Insert(insertionIndex, propertyToInsert);
        var newClassDeclaration = classDeclaration.WithMembers(newMembers);

        // Check if we need to add using directives
        var compilationUnit = root as CompilationUnitSyntax;
        CompilationUnitSyntax? newCompilationUnit = null;

        if (compilationUnit != null)
        {
            var usingsToAdd = new System.Collections.Generic.List<UsingDirectiveSyntax>();

            // Check for System.Threading
            var hasSystemThreadingUsing = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "System.Threading");

            if (!hasSystemThreadingUsing)
            {
                usingsToAdd.Add(SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("Threading"))));
            }

            // Check for BenchmarkDotNet.Attributes
            var hasBenchmarkDotNetAttributesUsing = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "BenchmarkDotNet.Attributes");

            if (!hasBenchmarkDotNetAttributesUsing)
            {
                usingsToAdd.Add(SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("BenchmarkDotNet"),
                        SyntaxFactory.IdentifierName("Attributes"))));
            }

            if (usingsToAdd.Count > 0)
            {
                newCompilationUnit = compilationUnit.AddUsings(usingsToAdd.ToArray());
            }
        }

        var newRoot = newCompilationUnit != null
            ? newCompilationUnit.ReplaceNode(newCompilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.ValueText == classDeclaration.Identifier.ValueText), newClassDeclaration)
            : root.ReplaceNode(classDeclaration, newClassDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }
}
