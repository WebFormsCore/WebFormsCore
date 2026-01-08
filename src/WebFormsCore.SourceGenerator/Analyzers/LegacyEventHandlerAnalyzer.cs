using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WebFormsCore.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LegacyEventHandlerAnalyzer : DiagnosticAnalyzer
{
    public const string LegacyOverrideDiagnosticId = "WFC0002";
    public const string LegacyPageEventDiagnosticId = "WFC0003";
    public const string ConflictingEventsDiagnosticId = "WFC0004";

    private static readonly DiagnosticDescriptor LegacyOverrideRule = new DiagnosticDescriptor(
        LegacyOverrideDiagnosticId,
        "Legacy event handler should be converted to async",
        "Method '{0}' should be converted to async event handler '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LegacyPageEventRule = new DiagnosticDescriptor(
        LegacyPageEventDiagnosticId,
        "Legacy page event handler should be converted to async",
        "Method '{0}' should be converted to async event handler '{1}'",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingEventsRule = new DiagnosticDescriptor(
        ConflictingEventsDiagnosticId,
        "Conflicting event handlers",
        "Method '{0}' conflicts with existing async event handler '{1}'. Remove the legacy handler.",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        LegacyOverrideRule,
        LegacyPageEventRule,
        ConflictingEventsRule);

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

    // Maps Page_X methods to their corresponding override methods
    private static readonly Dictionary<string, string> PageToOverrideMethods = new()
    {
        { "Page_PreInit", "OnPreInit" },
        { "Page_Init", "OnInit" },
        { "Page_Load", "OnLoad" },
        { "Page_PreRender", "OnPreRender" },
        { "Page_Unload", "OnUnload" }
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
        var methodName = methodDeclaration.Identifier.Text;

        var model = context.SemanticModel;
        var methodSymbol = model.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol == null) return;

        if (!IsSubclassOf(methodSymbol.ContainingType, "WebFormsCore.UI.Control")) return;

        var containingType = methodSymbol.ContainingType;

        // Check for legacy override methods (OnInit, OnLoad, etc.)
        if (LegacyOverrideMethods.TryGetValue(methodName, out var asyncMethodName))
        {
            if (methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                // Check if async version already exists
                if (HasMethod(containingType, asyncMethodName))
                {
                    // If async version exists, this should just be removed or merged
                    return;
                }

                // Check method signature: protected override void OnInit(EventArgs e)
                if (IsLegacyOverrideSignature(methodSymbol))
                {
                    var diagnostic = Diagnostic.Create(LegacyOverrideRule,
                        methodDeclaration.Identifier.GetLocation(),
                        methodName,
                        asyncMethodName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Check for legacy Page_X methods
        if (LegacyPageMethods.TryGetValue(methodName, out asyncMethodName))
        {
            // Check if the corresponding override method exists (OnInit for Page_Init)
            if (PageToOverrideMethods.TryGetValue(methodName, out var overrideMethodName))
            {
                if (HasOverrideMethod(containingType, overrideMethodName))
                {
                    // OnInit exists, skip Page_Init - it will be converted together
                    return;
                }
            }

            // Check if async version already exists
            if (HasMethod(containingType, asyncMethodName))
            {
                // Conflict: both Page_Init and OnInitAsync exist
                var diagnostic = Diagnostic.Create(ConflictingEventsRule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodName,
                    asyncMethodName);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Check method signature: protected void Page_Init(object sender, EventArgs e)
            if (IsLegacyPageEventSignature(methodSymbol))
            {
                var diagnostic = Diagnostic.Create(LegacyPageEventRule,
                    methodDeclaration.Identifier.GetLocation(),
                    methodName,
                    asyncMethodName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsLegacyOverrideSignature(IMethodSymbol method)
    {
        // protected override void OnInit(EventArgs e)
        if (method.ReturnsVoid &&
            method.Parameters.Length == 1 &&
            method.Parameters[0].Type.ToDisplayString() == "System.EventArgs")
        {
            return true;
        }

        return false;
    }

    private static bool IsLegacyPageEventSignature(IMethodSymbol method)
    {
        // protected void Page_Init(object sender, EventArgs e)
        if (method.ReturnsVoid &&
            method.Parameters.Length == 2 &&
            method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
            method.Parameters[1].Type.ToDisplayString() == "System.EventArgs")
        {
            return true;
        }

        return false;
    }

    private static bool HasMethod(INamedTypeSymbol type, string methodName)
    {
        return type.GetMembers(methodName).OfType<IMethodSymbol>().Any();
    }

    private static bool HasOverrideMethod(INamedTypeSymbol type, string methodName)
    {
        return type.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Any(m => m.IsOverride);
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

