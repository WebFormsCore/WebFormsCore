using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using WebFormsCore.SourceGenerator.Analyzers;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class ControlEventHandlerAnalyzerTest
{
    [Fact]
    public async Task Warning_If_BaseCall_Is_Missing()
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
                    protected override async ValueTask {|#0:OnInitAsync|}(CancellationToken token)
                    {
                        // Missing base.OnInitAsync(token)
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Warning_If_Branch_Is_Missing_BaseCall()
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
                    protected override async ValueTask {|#0:OnInitAsync|}(CancellationToken token)
                    {
                        if (token.IsCancellationRequested)
                        {
                            await base.OnInitAsync(token);
                        }
                        else
                        {
                            // ERROR!
                        }
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Warning_If_Switch_Is_Missing_BaseCall_In_Case()
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
                    protected override async ValueTask {|#0:OnInitAsync|}(CancellationToken token)
                    {
                        switch (token.IsCancellationRequested)
                        {
                            case true:
                                await base.OnInitAsync(token);
                                break;
                            case false:
                                // ERROR!
                                break;
                            default:
                                await base.OnInitAsync(token);
                                break;
                        }
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task Warning_If_Switch_Is_Missing_Default()
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
                    protected override async ValueTask {|#0:OnInitAsync|}(CancellationToken token)
                    {
                        switch (token.IsCancellationRequested)
                        {
                            case true:
                            case false:
                                await base.OnInitAsync(token);
                                break;
                        }
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task No_Warning_If_All_Branches_Have_BaseCall()
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
                        if (token.IsCancellationRequested)
                        {
                            await base.OnInitAsync(token);
                        }
                        else
                        {
                            await base.OnInitAsync(token);
                        }
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_If_BaseCall_Is_Before_Branch()
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
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_If_BaseCall_In_OnInitAsync()
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
    public async Task No_Warning_If_BaseCall_With_ConfigureAwait()
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
                        await base.OnInitAsync(token).ConfigureAwait(false);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_If_Sync_Return_BaseCall()
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
                    protected override ValueTask OnPreInitAsync(CancellationToken token)
                    {
                        return base.OnPreInitAsync(token);
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_If_Expression_Bodied_BaseCall()
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
                    protected override ValueTask OnPreInitAsync(CancellationToken token)
                        => base.OnPreInitAsync(token);
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task No_Warning_In_RenderAsync()
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
                    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
                    {
                        // Missing base.RenderAsync(writer, token)
                    }
                }
            }
            """;

        await VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task Warning_If_Early_Return_Before_BaseCall()
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
                    protected override async ValueTask {|#0:OnInitAsync|}(CancellationToken token)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return; // Early return - base never called
                        }
                        await base.OnInitAsync(token);
                    }
                }
            }
            """;

        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("OnInitAsync");

        await VerifyAnalyzerAsync(code, expected);
    }

    private static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<ControlEventHandlerAnalyzer, DefaultVerifier>
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
