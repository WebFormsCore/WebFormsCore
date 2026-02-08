using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using WebFormsCore.SourceGenerator.Analyzers;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class LegacyEventHandlerCodeFixTest
{
    #region Code Fix Tests - Legacy Page_X Methods

    [Fact]
    public async Task CodeFix_Converts_Page_Init_To_OnInitAsync()
    {
        const string code =
            """
            using WebFormsCore.UI;
            using System;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void {|#0:Page_Init|}(object sender, EventArgs e)
                    {
                        InitializeComponent();
                    }

                    private void InitializeComponent() { }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected override async ValueTask OnInitAsync(CancellationToken token)
                    {
                        await base.OnInitAsync(token);
                        InitializeComponent();
                    }

                    private void InitializeComponent() { }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Init", "OnInitAsync");

        await VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Converts_Page_Load_To_OnLoadAsync()
    {
        const string code =
            """
            using WebFormsCore.UI;
            using System;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void {|#0:Page_Load|}(object sender, EventArgs e)
                    {
                        LoadData();
                    }

                    private void LoadData() { }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected override async ValueTask OnLoadAsync(CancellationToken token)
                    {
                        await base.OnLoadAsync(token);
                        LoadData();
                    }

                    private void LoadData() { }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Load", "OnLoadAsync");

        await VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Page_Init_Adds_Base_Call()
    {
        // Page_Init doesn't have base call in original, but async version MUST have it
        const string code =
            """
            using WebFormsCore.UI;
            using System;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void {|#0:Page_Init|}(object sender, EventArgs e)
                    {
                        DoSomething();
                    }

                    private void DoSomething() { }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected override async ValueTask OnInitAsync(CancellationToken token)
                    {
                        await base.OnInitAsync(token);
                        DoSomething();
                    }

                    private void DoSomething() { }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Init", "OnInitAsync");

        await VerifyCodeFixAsync(code, expected, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Page_PreRender_To_OnPreRenderAsync()
    {
        const string code =
            """
            using WebFormsCore.UI;
            using System;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void {|#0:Page_PreRender|}(object sender, EventArgs e)
                    {
                        SetupView();
                    }

                    private void SetupView() { }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
                    {
                        await base.OnPreRenderAsync(token);
                        SetupView();
                    }

                    private void SetupView() { }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_PreRender", "OnPreRenderAsync");

        await VerifyCodeFixAsync(code, expected, fixedCode);
    }

    #endregion

    #region Code Fix Tests - Preserves Using Statements

    [Fact]
    public async Task CodeFix_Does_Not_Duplicate_Using_Statements()
    {
        const string code =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void {|#0:Page_Init|}(object sender, EventArgs e)
                    {
                        Setup();
                    }

                    private void Setup() { }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected override async ValueTask OnInitAsync(CancellationToken token)
                    {
                        await base.OnInitAsync(token);
                        Setup();
                    }

                    private void Setup() { }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Init", "OnInitAsync");

        await VerifyCodeFixAsync(code, expected, fixedCode);
    }

    #endregion

    private static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        var test = new CSharpCodeFixTest<LegacyEventHandlerAnalyzer, LegacyEventHandlerCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = new ReferenceAssemblies(
                targetFramework: "net10.0",
                referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
                referenceAssemblyPath: Path.Combine("ref", "net10.0"))
        };

        // Configure EditorConfig to force LF line endings for cross-platform compatibility
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", """
            root = true
            
            [*]
            end_of_line = lf
            
            """));
        test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", """
            root = true
            
            [*]
            end_of_line = lf
            
            """));

        test.TestState.AdditionalReferences.Add(typeof(Control).Assembly);
        test.ExpectedDiagnostics.Add(expected);

        await test.RunAsync();
    }
}
