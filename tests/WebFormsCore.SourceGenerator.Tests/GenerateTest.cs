using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using VerifyTests;
using VerifyXunit;
using WebFormsCore.SourceGenerator.Tests.Utils;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class GenerateTest
{
    [SkippableFact]
    public Task GenerateDesigner()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Line endings are different");

        var generator = new CSharpDesignGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore.UI;
            using WebFormsCore.UI.WebControls;
            using WebFormsCore.UI.Attributes;

            namespace Tests
            {
                public class TestItem
                {
                }

                public partial class PageTest : Page
                {
                    public string Test = "foo";
                
                    public HtmlForm form1;
                }

                public interface IService
                {

                }

                [ParseChildren(true)]
                public partial class ControlTest
                {
                    private readonly IService _service;

                    public ControlTest(IService service)
                    {
                        _service = service;
                    }

                    public ITemplate Template { get; set; }

                    protected void btnIncrement_OnClick(object? sender, EventArgs e)
                    {

                    }
                }

                public class ControlTest<T> : ControlTest
                {

                }
            }
            """
        );

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] {syntaxTree},
            references: references);

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "Example.aspx",
                    """
                    <%@ Page language="C#" Inherits="Tests.PageTest" EnableViewState="false" %>
                    <%@ Register TagPrefix="app" Namespace="Tests" %>
                    <!DOCTYPE htm>
                    <html>
                    <head runat="server">
                        <title></title>
                    </head>
                    <body>
                        <%= Test %>

                        <form id="form1" runat="server">
                            <div>
                                <asp:TextBox id="tbUsername" runat="server" Text="<%# Test %>" /><br />
                                <asp:textbox id="tbPassword" runat="server" /><br />
                                <asp:button id="btnLogin" runat="server" click="btnLogin_Click" text="Login" />
                            </div>
                        </form>
                    </body>
                    </html>
                    """
                ),
                new MemoryAdditionalText(
                    "Example.ascx",
                    """
                    <%@ Control language="C#" Inherits="Tests.ControlTest" %>

                    <asp:Literal id="litTest" runat="server" />
                    <asp:Button runat="server" ID="btnIncrement" OnClick="btnIncrement_OnClick">Increment</asp:Button>
                    """
                )
            )
        );

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "web.config",
                    """
                    <configuration>
                        <system.web>
                            <pages>
                                <controls>
                                    <add tagPrefix="asp" namespace="WebFormsCore.UI.WebControls" />
                                    <add tagPrefix="asp" namespace="WebFormsCore.UI.HtmlControls" />
                                </controls>
                            </pages>
                        </system.web>
                    </configuration>
                    """
                )
            )
        );

        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }

    [SkippableFact]
    public Task GenerateViewState()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Line endings are different");

        var generator = new ViewStateGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System.Web;

            namespace Tests
            {
                public partial class Example
                {
                    [ViewState] private string test;
                    [ViewState(nameof(Validate)] private string test2;

                    public bool Validate => true;
                }
            }
            """
        );

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }

    [SkippableFact]
    public Task GenerateDesignerVisualBasic()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Line endings are different");

        var generator = new VisualBasicDesignGenerator();
        
        GeneratorDriver driver = VisualBasicGeneratorDriver.Create(
            ImmutableArray.Create(
                generator.AsSourceGenerator()
            )
        );

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = VisualBasicSyntaxTree.ParseText(
            """
            Imports WebFormsCore

            Partial Class DefaultPage
                Inherits UI.Page

                Protected Sub btnAdd_OnClick(sender As Object, e As EventArgs)
                End Sub
            End Class
            """
        );

        var compilation = VisualBasicCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "DefaultPage.aspx",
                    """
                    <%@ Page language="VB" Inherits="Tests.DefaultPage" %>
                    <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                    <!DOCTYPE html>
                    <html lang="en">
                    <body id="Body" runat="server">

                        <div class="container">
                            <div class="mt-4">
                                <form runat="server" method="post">
                                    <wfc:Button runat="server" ID="btnAdd" OnClick="btnAdd_OnClick">Add</wfc:Button>
                                    <wfc:Repeater runat="server" ID="rptItems">
                                        <ItemTemplate>
                                            <% If True Then %>
                                                <wfc:Literal runat="server" ID="litItem" />
                                            <% End If %>
                                        </ItemTemplate>
                                    </wfc:Repeater>
                                </form>
                            </div>
                        </div>

                    </body>
                    </html>
                    """
                )
            )
        );

        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }


    [Fact]
    public void UnknownPropertyWarning()
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            references: [MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)]);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CSharpDesignGenerator());

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI" %>

                <wfc:Control InvalidPropertyName="Test" runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostics[0].Severity);
        Assert.Equal("Could not find property 'InvalidPropertyName' on type 'WebFormsCore.UI.Control'", diagnostics[0].GetMessage());
    }
}
