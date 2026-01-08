using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using WebFormsCore.SourceGenerator.Analyzers;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class ControlEventHandlerCodeFixTest
{
    [Fact]
    public async Task CodeFix_Adds_BaseCall_To_Empty_Async_Method()
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
                    }
                }
            }
            """;

        const string fixedCode =
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

        await VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Adds_BaseCall_And_Async_To_NonAsync_Method()
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
                    protected override ValueTask {|#0:OnLoadAsync|}(CancellationToken token)
                    {
                        return default;
                    }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyControl : Control
                {
                    protected override async ValueTask OnLoadAsync(CancellationToken token)
                    {
                        await base.OnLoadAsync(token);
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Adds_BaseCall_And_Converts_ValueTaskCompletedTask()
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
                    protected override ValueTask {|#0:OnPreInitAsync|}(CancellationToken token)
                    {
                        return ValueTask.CompletedTask;
                    }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyControl : Control
                {
                    protected override async ValueTask OnPreInitAsync(CancellationToken token)
                    {
                        await base.OnPreInitAsync(token);
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Converts_ExpressionBody_To_Block()
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
                    protected override ValueTask {|#0:OnPreInitAsync|}(CancellationToken token)
                        => default;
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyControl : Control
                {
                    protected override async ValueTask OnPreInitAsync(CancellationToken token)
                    {
                        await base.OnPreInitAsync(token);
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(code, fixedCode);
    }

    [Fact]
    public async Task CodeFix_Replaces_Return_Default_With_Return_In_Branch()
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
                    protected override ValueTask {|#0:OnLoadAsync|}(CancellationToken token)
                    {
                        if (token.IsCancellationRequested)
                        {
                            return default;
                        }
                        return default;
                    }
                }
            }
            """;

        const string fixedCode =
            """
            using WebFormsCore.UI;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Tests
            {
                public class MyControl : Control
                {
                    protected override async ValueTask OnLoadAsync(CancellationToken token)
                    {
                        await base.OnLoadAsync(token);
                        if (token.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(code, fixedCode);
    }

    private static async Task VerifyCodeFixAsync(string source, string fixedSource)
    {
        var expected = new DiagnosticResult(ControlEventHandlerAnalyzer.DiagnosticId, DiagnosticSeverity.Error)
            .WithLocation(0);

        var test = new CSharpCodeFixTest<ControlEventHandlerAnalyzer, ControlEventHandlerCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = new ReferenceAssemblies(
                targetFramework: "net10.0",
                referenceAssemblyPackage: new PackageIdentity("Microsoft.NETCore.App.Ref", "10.0.0-rc.2.25502.107"),
                referenceAssemblyPath: Path.Combine("ref", "net10.0"))
        };

        test.TestState.AdditionalReferences.Add(typeof(Control).Assembly);
        test.TestState.ExpectedDiagnostics.Add(expected);
        test.FixedState.AdditionalReferences.Add(typeof(Control).Assembly);

        await test.RunAsync();
    }
}

