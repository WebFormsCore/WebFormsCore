using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WebFormsCore.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ControlEventHandlerAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "WFC0001";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        "Base event handler method should be called",
        "Method '{0}' should call its base implementation on all paths",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    private static readonly HashSet<string> Methods = new()
    {
        "OnFrameworkInitAsync",
        "OnPreInitAsync",
        "OnInitAsync",
        "OnLoadAsync",
        "OnPreRenderAsync",
        "OnUnloadAsync",
        "DataBindAsync"
    };

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        if (!methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword)) return;

        var methodName = methodDeclaration.Identifier.Text;
        if (!Methods.Contains(methodName)) return;

        var model = context.SemanticModel;
        var methodSymbol = model.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null) return;

        if (!IsSubclassOf(methodSymbol.ContainingType, "WebFormsCore.UI.Control")) return;

        if (methodDeclaration.Body == null && methodDeclaration.ExpressionBody == null) return;

        bool hasDefiniteBaseCall = false;

        if (methodDeclaration.Body != null)
        {
            hasDefiniteBaseCall = DefiniteBaseCall(methodDeclaration.Body, methodName);
        }
        else if (methodDeclaration.ExpressionBody != null)
        {
            hasDefiniteBaseCall = IsBaseCall(methodDeclaration.ExpressionBody.Expression, methodName);
        }

        if (!hasDefiniteBaseCall)
        {
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Checks if the base method is definitely called on all execution paths.
    /// Returns true only if ALL paths through the statement will call the base method.
    /// </summary>
    private bool DefiniteBaseCall(StatementSyntax statement, string methodName)
    {
        if (statement is BlockSyntax block)
        {
            // Track whether we've seen a definite base call that covers all remaining paths
            bool baseCallFound = false;

            foreach (var s in block.Statements)
            {
                // If we already found a base call, remaining statements don't matter
                if (baseCallFound)
                {
                    break;
                }

                // Check if this statement is a base call
                if (DefiniteBaseCall(s, methodName))
                {
                    baseCallFound = true;
                    continue;
                }

                // Check if this is an if without else that has an early exit
                // In this case, there's a path (the if-body) that exits without base call
                if (s is IfStatementSyntax ifWithEarlyExit && ifWithEarlyExit.Else == null
                    && HasTerminatingPath(ifWithEarlyExit.Statement))
                {
                    // The "if" has a path that returns/throws early.
                    // Check if that path calls base before terminating
                    if (!DefiniteBaseCall(ifWithEarlyExit.Statement, methodName))
                    {
                        // The early exit path doesn't call base, this is an error
                        return false;
                    }
                    // The early exit path DOES call base, we still need to check remaining
                    // statements for the non-terminating path
                    continue;
                }

                // If we hit a return/throw before finding base call, this path doesn't call base
                if (IsTerminatingStatement(s))
                {
                    return false;
                }
            }

            return baseCallFound;
        }

        if (statement is ExpressionStatementSyntax exprStmt)
        {
            return IsBaseCall(exprStmt.Expression, methodName);
        }

        if (statement is ReturnStatementSyntax returnStmt)
        {
            // Check if return statement itself calls base
            if (returnStmt.Expression != null && IsBaseCall(returnStmt.Expression, methodName))
            {
                return true;
            }
            // A return statement without base call means this path doesn't call base
            return false;
        }

        if (statement is ThrowStatementSyntax)
        {
            // A throw statement terminates the path - we consider it as "handled"
            // since the method won't complete normally anyway
            return true;
        }

        if (statement is IfStatementSyntax ifStmt)
        {
            // If there's no else, we need to check if the "if" body always terminates
            // If it does, we return false because we need base call after this if
            if (ifStmt.Else == null)
            {
                // If the if-body terminates (return/throw), this doesn't provide base call
                // but allows subsequent code to run on the false path
                return false;
            }
            return DefiniteBaseCall(ifStmt.Statement, methodName) &&
                   DefiniteBaseCall(ifStmt.Else.Statement, methodName);
        }

        if (statement is SwitchStatementSyntax switchStmt)
        {
            bool hasDefault = false;
            foreach (var section in switchStmt.Sections)
            {
                if (section.Labels.OfType<DefaultSwitchLabelSyntax>().Any()) hasDefault = true;
                bool sectionHasCall = false;
                foreach (var s in section.Statements)
                {
                    if (DefiniteBaseCall(s, methodName))
                    {
                        sectionHasCall = true;
                        break;
                    }
                }
                if (!sectionHasCall) return false;
            }
            return hasDefault;
        }

        if (statement is TryStatementSyntax tryStmt)
        {
            // If finally has base call, it's always called
            if (tryStmt.Finally != null && DefiniteBaseCall(tryStmt.Finally.Block, methodName))
            {
                return true;
            }

            // Otherwise, try block must have it AND all catch blocks must have it
            bool tryHasCall = DefiniteBaseCall(tryStmt.Block, methodName);
            if (!tryHasCall) return false;

            foreach (var catchClause in tryStmt.Catches)
            {
                if (!DefiniteBaseCall(catchClause.Block, methodName)) return false;
            }
            return true;
        }

        if (statement is LocalDeclarationStatementSyntax)
        {
            return false;
        }

        if (statement is UsingStatementSyntax usingStmt)
        {
            return DefiniteBaseCall(usingStmt.Statement, methodName);
        }

        if (statement is ForEachStatementSyntax)
        {
            // Loops don't guarantee execution, so we don't count base calls inside them
            return false;
        }

        if (statement is ForStatementSyntax || statement is WhileStatementSyntax || statement is DoStatementSyntax)
        {
            // Loops don't guarantee execution
            return false;
        }

        if (statement is LockStatementSyntax lockStmt)
        {
            return DefiniteBaseCall(lockStmt.Statement, methodName);
        }

        return false;
    }

    private static bool IsTerminatingStatement(StatementSyntax statement)
    {
        return statement is ReturnStatementSyntax or ThrowStatementSyntax;
    }

    private static bool HasTerminatingPath(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax or ThrowStatementSyntax)
        {
            return true;
        }

        if (statement is BlockSyntax block)
        {
            foreach (var s in block.Statements)
            {
                if (HasTerminatingPath(s)) return true;
            }
        }

        if (statement is IfStatementSyntax ifStmt)
        {
            return HasTerminatingPath(ifStmt.Statement) ||
                   (ifStmt.Else != null && HasTerminatingPath(ifStmt.Else.Statement));
        }

        return false;
    }

    private bool IsBaseCall(ExpressionSyntax expression, string methodName)
    {
        var current = expression;

        // Unwrap await expression
        if (current is AwaitExpressionSyntax awaitExpr)
        {
            current = awaitExpr.Expression;
        }

        // Unwrap chained method calls like .ConfigureAwait(false)
        while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax chainedAccess }
               && chainedAccess.Expression is not BaseExpressionSyntax)
        {
            current = chainedAccess.Expression;
        }

        if (current is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is BaseExpressionSyntax &&
            memberAccess.Name.Identifier.Text == methodName)
        {
            return true;
        }
        return false;
    }

    private static bool IsSubclassOf(INamedTypeSymbol? type, string targetType)
    {
        var current = type?.BaseType;
        while (current != null)
        {
            var displayString = current.ToDisplayString();
            if (displayString == targetType)
                return true;
            current = current.BaseType;
        }
        return false;
    }
}
