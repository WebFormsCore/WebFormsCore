using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebFormsCore.SourceGenerator.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ControlEventHandlerCodeFixProvider)), Shared]
public class ControlEventHandlerCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ControlEventHandlerAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>().FirstOrDefault();

        if (methodDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add base method call",
                createChangedDocument: c => AddBaseCallAsync(context.Document, methodDeclaration, c),
                equivalenceKey: "AddBaseMethodCall"),
            diagnostic);
    }

    private static async Task<Document> AddBaseCallAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var methodName = methodDeclaration.Identifier.Text;
        var parameterName = methodDeclaration.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? "token";

        // Detect indentation from the method declaration
        var methodLeadingTrivia = methodDeclaration.GetLeadingTrivia();
        var methodIndent = GetIndentation(methodLeadingTrivia);
        var bodyIndent = methodIndent + "    ";

        // Build the base call: await base.OnXAsync(token);
        var baseCall = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.BaseExpression(),
                        SyntaxFactory.IdentifierName(methodName)))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameterName)))))))
            .WithLeadingTrivia(SyntaxFactory.Whitespace(bodyIndent))
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        MethodDeclarationSyntax newMethod;

        if (methodDeclaration.ExpressionBody != null)
        {
            // Convert expression body to block body with base call
            var existingExpression = methodDeclaration.ExpressionBody.Expression;
            var statements = new List<StatementSyntax> { baseCall };

            // Check if the expression is a return-worthy expression or just an await
            if (IsCompletedTaskReturn(existingExpression))
            {
                // Expression like ValueTask.CompletedTask or default - just remove it
            }
            else if (existingExpression is AwaitExpressionSyntax)
            {
                // Keep the await as a statement
                statements.Add(SyntaxFactory.ExpressionStatement(existingExpression)
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(bodyIndent))
                    .WithTrailingTrivia(SyntaxFactory.LineFeed));
            }
            else
            {
                // Keep as return statement
                statements.Add(SyntaxFactory.ReturnStatement(existingExpression)
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(bodyIndent))
                    .WithTrailingTrivia(SyntaxFactory.LineFeed));
            }

            var block = SyntaxFactory.Block(statements)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(methodIndent))
                    .WithTrailingTrivia(SyntaxFactory.LineFeed))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(methodIndent))
                    .WithTrailingTrivia(SyntaxFactory.LineFeed));

            // Remove trailing whitespace/newlines from the method signature before adding the body
            var cleanedMethod = methodDeclaration
                .WithExpressionBody(null)
                .WithSemicolonToken(default);

            // Get the parameter list and ensure it has proper trivia
            var paramList = cleanedMethod.ParameterList;
            if (paramList.CloseParenToken.TrailingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia) || t.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                paramList = paramList.WithCloseParenToken(
                    paramList.CloseParenToken.WithTrailingTrivia(SyntaxFactory.LineFeed));
                cleanedMethod = cleanedMethod.WithParameterList(paramList);
            }

            newMethod = cleanedMethod.WithBody(block);
        }
        else if (methodDeclaration.Body != null)
        {
            // Insert base call at the beginning, and clean up return statements
            var existingStatements = methodDeclaration.Body.Statements;
            var newStatements = new List<StatementSyntax> { baseCall };

            for (int i = 0; i < existingStatements.Count; i++)
            {
                var statement = existingStatements[i];
                var isLast = i == existingStatements.Count - 1;

                var processedStatement = ProcessStatement(statement, isLast);
                if (processedStatement != null)
                {
                    newStatements.Add(processedStatement);
                }
            }

            var newBody = methodDeclaration.Body.WithStatements(SyntaxFactory.List(newStatements));
            newMethod = methodDeclaration.WithBody(newBody);
        }
        else
        {
            return document;
        }

        // Ensure method is async
        if (!newMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            var asyncKeyword = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);

            // Find the position to insert async (after access modifiers, before return type)
            var modifiers = newMethod.Modifiers;
            var newModifiers = new List<SyntaxToken>();

            bool asyncInserted = false;
            foreach (var modifier in modifiers)
            {
                newModifiers.Add(modifier);
                // Insert async after override
                if (modifier.IsKind(SyntaxKind.OverrideKeyword) && !asyncInserted)
                {
                    newModifiers.Add(asyncKeyword);
                    asyncInserted = true;
                }
            }

            if (!asyncInserted)
            {
                newModifiers.Add(asyncKeyword);
            }

            newMethod = newMethod.WithModifiers(SyntaxFactory.TokenList(newModifiers));
        }

        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        return document.WithSyntaxRoot(newRoot);
    }

    private static StatementSyntax? ProcessStatement(StatementSyntax statement, bool isLast)
    {
        // Handle return statements with ValueTask.CompletedTask or default
        if (statement is ReturnStatementSyntax returnStmt && returnStmt.Expression != null)
        {
            if (IsCompletedTaskReturn(returnStmt.Expression))
            {
                if (isLast)
                {
                    // Last statement - remove it entirely
                    return null;
                }
                else
                {
                    // Not last - convert to just 'return;'
                    return SyntaxFactory.ReturnStatement()
                        .WithLeadingTrivia(statement.GetLeadingTrivia())
                        .WithTrailingTrivia(statement.GetTrailingTrivia());
                }
            }
        }

        // Handle blocks recursively (for if/else, etc.)
        if (statement is BlockSyntax block)
        {
            var newStatements = new List<StatementSyntax>();
            for (int i = 0; i < block.Statements.Count; i++)
            {
                var s = block.Statements[i];
                var blockIsLast = i == block.Statements.Count - 1;
                var processed = ProcessStatement(s, isLast && blockIsLast);
                if (processed != null)
                {
                    newStatements.Add(processed);
                }
            }
            return block.WithStatements(SyntaxFactory.List(newStatements));
        }

        // Handle if statements
        if (statement is IfStatementSyntax ifStmt)
        {
            var newThen = ProcessStatement(ifStmt.Statement, isLast);
            var newElse = ifStmt.Else != null
                ? SyntaxFactory.ElseClause(ProcessStatement(ifStmt.Else.Statement, isLast) ?? SyntaxFactory.Block())
                : null;

            return ifStmt
                .WithStatement(newThen ?? SyntaxFactory.Block())
                .WithElse(newElse);
        }

        return statement;
    }

    private static bool IsCompletedTaskReturn(ExpressionSyntax expression)
    {
        // Check for 'default'
        if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.DefaultLiteralExpression))
        {
            return true;
        }

        // Check for 'default(ValueTask)'
        if (expression is DefaultExpressionSyntax)
        {
            return true;
        }

        // Check for 'ValueTask.CompletedTask'
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            var name = memberAccess.Name.Identifier.Text;
            if (name == "CompletedTask")
            {
                return true;
            }
        }

        return false;
    }

    private static string GetIndentation(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList.Reverse())
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return trivia.ToString();
            }
        }
        return "        "; // Default to 8 spaces
    }
}

