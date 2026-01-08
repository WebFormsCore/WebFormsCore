using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using WebFormsCore.SourceGenerator.Analyzers;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class LegacyEventHandlerAnalyzerTest
{
    #region Analyzer Tests - Legacy Page_X Methods

    [Fact]
    public async Task Warning_If_Legacy_Page_Init()
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
                        // custom logic
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Init", "OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Warning_If_Legacy_Page_Load()
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
                        // custom logic
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_Load", "OnLoadAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Warning_If_Legacy_Page_PreRender()
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
                        // custom logic
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.LegacyPageEventDiagnosticId, DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Page_PreRender", "OnPreRenderAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    #endregion

    #region Analyzer Tests - No Warning Cases

    [Fact]
    public async Task No_Warning_If_Already_Async_OnInitAsync()
    {
        const string code =
            """
            using WebFormsCore.UI;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyControl : Control
                {
                    protected override async ValueTask OnInitAsync(CancellationToken token)
                    {
                        await base.OnInitAsync(token);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_If_Page_Init_Has_Wrong_Signature()
    {
        // Page_Init with wrong signature should not trigger warning
        const string code =
            """
            using WebFormsCore.UI;
            using System;

            namespace Tests
            {
                public class MyPage : Page
                {
                    protected void Page_Init()
                    {
                        // wrong signature - no parameters
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    #endregion

    #region Analyzer Tests - Conflicting Events

    [Fact]
    public async Task Error_If_Page_Init_And_OnInitAsync_Both_Exist()
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
                        // This conflicts with OnInitAsync
                    }

                    protected override async ValueTask OnInitAsync(CancellationToken token)
                    {
                        await base.OnInitAsync(token);
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.ConflictingEventsDiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("Page_Init", "OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Error_If_Page_Load_And_OnLoadAsync_Both_Exist()
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
                    protected void {|#0:Page_Load|}(object sender, EventArgs e)
                    {
                        // This conflicts with OnLoadAsync
                    }

                    protected override async ValueTask OnLoadAsync(CancellationToken token)
                    {
                        await base.OnLoadAsync(token);
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(LegacyEventHandlerAnalyzer.ConflictingEventsDiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("Page_Load", "OnLoadAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    #endregion

    private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<LegacyEventHandlerAnalyzer, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = new ReferenceAssemblies(
                targetFramework: "net10.0",
                referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
                referenceAssemblyPath: Path.Combine("ref", "net10.0"))
        };

        test.TestState.AdditionalReferences.Add(typeof(Control).Assembly);
        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync();
    }
}
