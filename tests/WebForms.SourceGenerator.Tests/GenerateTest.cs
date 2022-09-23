using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTests;
using VerifyXunit;
using WebForms.SourceGenerator.Tests.Utils;
using WebFormsCore.UI;

namespace WebForms.SourceGenerator.Tests;

[UsesVerify]
public class GenerateTest
{
    [Fact]
    public Task GenerateDesigner()
    {
        VerifySourceGenerators.Enable();

        var generator = new DesignerGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore.UI;
            using WebFormsCore.UI.WebControls;

            namespace Tests
            {
                public class TestItem
                {
                }

                public partial class PageTest
                {
                    public HtmlForm form1;
                }

                public partial class ControlTest
                {
                    public ITemplate Template { get; set; }
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
                    <%@ Page language="C#" Inherits="Tests.PageTest" %>
                    <%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" %>
                    <%@ Register TagPrefix="app" Namespace="Tests" %>
                    <!DOCTYPE htm>
                    <html>
                    <head runat="server">
                        <title></title>
                    </head>
                    <body>
                        <script runat="server">
                            private string Test => "Test";
                        </script>

                        <%= Test %>

                        <form id="form1" runat="server">
                            <div>
                                <app:ControlTest ItemType="Tests.TestItem" runat="server">
                                    <Template>Test</Template>
                                </app:ControlTest><br />
                                <asp:TextBox id="tbUsername" runat="server" /><br />
                                <asp:textbox id="tbPassword" runat="server" /><br />
                                <asp:button id="btnLogin" runat="server" click="btnLogin_Click" text="Login" />
                            </div>
                        </form>
                    </body>
                    </html>
                    """
                )
            )
        );

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "Example.ascx",
                    """
                    <%@ Control language="C#" Inherits="Tests.ControlTest" %>
                    <%@ Register TagPrefix="asp" Namespace="WebFormsCore.UI.WebControls" %>

                    <asp:Literal id="litTest" runat="server" />
                    """
                )
            )
        );

        driver = driver.RunGenerators(compilation);

        return Verifier.Verify(driver);
    }


    [Fact]
    public Task GenerateViewState()
    {
        VerifySourceGenerators.Enable();

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
}
