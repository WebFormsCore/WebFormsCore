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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LegacyEventHandlerCodeFixProvider)), Shared]
public class LegacyEventHandlerCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        LegacyEventHandlerAnalyzer.LegacyOverrideDiagnosticId,
        LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    // Maps legacy override methods to their async equivalents
    private static readonly Dictionary<string, string> LegacyOverrideMethods = new()
    {
        { "OnPreInit", "OnPreInitAsync" },
        { "OnInit", "OnInitAsync" },
        { "OnLoad", "OnLoadAsync" },
        { "OnPreRender", "OnPreRenderAsync" },
        { "OnUnload", "OnUnloadAsync" }
    };

    // Maps legacy Page_X methods to their async equivalents
    private static readonly Dictionary<string, string> LegacyPageMethods = new()
    {
        { "Page_PreInit", "OnPreInitAsync" },
        { "Page_Init", "OnInitAsync" },
        { "Page_Load", "OnLoadAsync" },
        { "Page_PreRender", "OnPreRenderAsync" },
        { "Page_Unload", "OnUnloadAsync" }
    };

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>().FirstOrDefault();

        if (methodDeclaration == null) return;

        // Only provide code fix for legacy methods that can be converted to async
        // Conflicting events (WFC0004) should be manually resolved by the developer
        if (diagnostic.Id == LegacyEventHandlerAnalyzer.LegacyOverrideDiagnosticId ||
            diagnostic.Id == LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Convert to async event handler",
                    createChangedDocument: c => ConvertToAsyncAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: "ConvertToAsyncEventHandler"),
                diagnostic);
        }
    }

    private static async Task<Document> ConvertToAsyncAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var methodName = methodDeclaration.Identifier.Text;
        var isOverrideMethod = LegacyOverrideMethods.ContainsKey(methodName);
        var isPageMethod = LegacyPageMethods.ContainsKey(methodName);

        if (!isOverrideMethod && !isPageMethod) return document;

        var asyncMethodName = isOverrideMethod
            ? LegacyOverrideMethods[methodName]
            : LegacyPageMethods[methodName];

        // Get indentation
        var methodLeadingTrivia = methodDeclaration.GetLeadingTrivia();
        var methodIndent = GetIndentation(methodLeadingTrivia);
        var bodyIndent = methodIndent + "    ";

        // Build the new async method
        var newMethod = CreateAsyncMethod(methodDeclaration, asyncMethodName, methodIndent, bodyIndent, isPageMethod);

        // Add required using statements if needed
        var newRoot = root.ReplaceNode(methodDeclaration, newMethod);
        newRoot = AddUsingDirectivesIfNeeded(newRoot);

        return document.WithSyntaxRoot(newRoot);
    }

    private static MethodDeclarationSyntax CreateAsyncMethod(
        MethodDeclarationSyntax originalMethod,
        string asyncMethodName,
        string methodIndent,
        string bodyIndent,
        bool isPageMethod)
    {
        // Create the parameter: CancellationToken token
        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("token"))
            .WithType(SyntaxFactory.ParseTypeName("CancellationToken").WithTrailingTrivia(SyntaxFactory.Space));

        // Create modifiers: protected override async
        var modifiers = SyntaxFactory.TokenList(
            SyntaxFactory.Token(SyntaxKind.ProtectedKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            SyntaxFactory.Token(SyntaxKind.OverrideKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Space));

        // Create the base call: await base.OnXAsync(token);
        var baseCall = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AwaitExpression(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.BaseExpression(),
                        SyntaxFactory.IdentifierName(asyncMethodName)))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")))))))
            .WithLeadingTrivia(SyntaxFactory.Whitespace(bodyIndent))
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        // Get the existing method body statements
        var existingStatements = new List<StatementSyntax>();

        if (originalMethod.Body != null)
        {
            foreach (var statement in originalMethod.Body.Statements)
            {
                var convertedStatement = ConvertStatementToAsync(statement, bodyIndent);
                if (convertedStatement != null)
                {
                    existingStatements.Add(convertedStatement);
                }
            }
        }
        else if (originalMethod.ExpressionBody != null)
        {
            var expr = originalMethod.ExpressionBody.Expression;
            if (!IsBaseCallExpression(expr, originalMethod.Identifier.Text))
            {
                existingStatements.Add(
                    SyntaxFactory.ExpressionStatement(expr)
                        .WithLeadingTrivia(SyntaxFactory.Whitespace(bodyIndent))
                        .WithTrailingTrivia(SyntaxFactory.LineFeed));
            }
        }

        // Create the body with base call first, then the converted statements
        var statements = new List<StatementSyntax> { baseCall };
        statements.AddRange(existingStatements);

        var body = SyntaxFactory.Block(statements)
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken)
                .WithLeadingTrivia(SyntaxFactory.Whitespace(methodIndent))
                .WithTrailingTrivia(SyntaxFactory.LineFeed))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                .WithLeadingTrivia(SyntaxFactory.Whitespace(methodIndent))
                .WithTrailingTrivia(SyntaxFactory.LineFeed));

        // Create the new method
        var newMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName("ValueTask").WithTrailingTrivia(SyntaxFactory.Space),
                SyntaxFactory.Identifier(asyncMethodName))
            .WithModifiers(modifiers)
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SingletonSeparatedList(parameter))
                .WithTrailingTrivia(SyntaxFactory.LineFeed))
            .WithBody(body)
            .WithLeadingTrivia(originalMethod.GetLeadingTrivia());

        return newMethod;
    }

    private static StatementSyntax? ConvertStatementToAsync(StatementSyntax statement, string indent)
    {
        // Skip base.OnInit(e) calls - they're being replaced with base.OnInitAsync(token)
        if (statement is ExpressionStatementSyntax exprStmt)
        {
            if (IsLegacyBaseCall(exprStmt.Expression))
            {
                return null;
            }
        }

        // Preserve the statement with proper indentation
        return statement
            .WithLeadingTrivia(SyntaxFactory.Whitespace(indent))
            .WithTrailingTrivia(SyntaxFactory.LineFeed);
    }

    private static bool IsLegacyBaseCall(ExpressionSyntax expression)
    {
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is BaseExpressionSyntax)
        {
            var methodName = memberAccess.Name.Identifier.Text;
            return LegacyOverrideMethods.ContainsKey(methodName);
        }
        return false;
    }

    private static bool IsBaseCallExpression(ExpressionSyntax expression, string methodName)
    {
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is BaseExpressionSyntax &&
            memberAccess.Name.Identifier.Text == methodName)
        {
            return true;
        }
        return false;
    }

    private static SyntaxNode AddUsingDirectivesIfNeeded(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit) return root;

        var usings = compilationUnit.Usings;
        var hasThreading = usings.Any(u => u.Name!.ToString() == "System.Threading");
        var hasTasks = usings.Any(u => u.Name!.ToString() == "System.Threading.Tasks");

        var newUsings = new List<UsingDirectiveSyntax>();

        if (!hasThreading)
        {
            newUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading"))
                .WithTrailingTrivia(SyntaxFactory.LineFeed));
        }

        if (!hasTasks)
        {
            newUsings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"))
                .WithTrailingTrivia(SyntaxFactory.LineFeed));
        }

        if (newUsings.Count > 0)
        {
            return compilationUnit.AddUsings(newUsings.ToArray());
        }

        return root;
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

