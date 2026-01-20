using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.SourceGenerator.Tests.Utils;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

/// <summary>
/// Tests for all parser diagnostic warnings (WFC0001-WFC0007).
/// </summary>
public class ParserDiagnosticsTest
{
    private static CSharpCompilation CreateCompilation(string code = "")
    {
        var syntaxTrees = new List<SyntaxTree>();

        if (!string.IsNullOrEmpty(code))
        {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(code));
        }

        return CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            references: [MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)]);
    }

    private static GeneratorDriver CreateDriver()
    {
        return CSharpGeneratorDriver.Create(new CSharpDesignGenerator());
    }

    #region WFC0002 - PropertyNotFound

    /// <summary>
    /// WFC0002: Tests that an unknown property on a control generates a warning.
    /// </summary>
    [Fact]
    public void WFC0002_PropertyNotFound_OnControl()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

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

        var wfc0002 = diagnostics.Where(d => d.Id == "WFC0002").ToList();
        Assert.Single(wfc0002);
        Assert.Equal(DiagnosticSeverity.Warning, wfc0002[0].Severity);
        Assert.Contains("InvalidPropertyName", wfc0002[0].GetMessage());
        Assert.Contains("WebFormsCore.UI.Control", wfc0002[0].GetMessage());
    }

    /// <summary>
    /// WFC0002: Tests that an unknown property on a Page directive generates a warning.
    /// </summary>
    [Fact]
    public void WFC0002_PropertyNotFound_OnPageDirective()
    {
        var compilation = CreateCompilation("""
            namespace Tests
            {
                public partial class TestPage : WebFormsCore.UI.Page { }
            }
            """);

        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.aspx",
                """
                <%@ Page language="C#" Inherits="Tests.TestPage" UnknownDirectiveProperty="Value" %>
                <html><body></body></html>
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0002 = diagnostics.Where(d => d.Id == "WFC0002").ToList();
        Assert.Single(wfc0002);
        Assert.Contains("UnknownDirectiveProperty", wfc0002[0].GetMessage());
    }

    /// <summary>
    /// WFC0002: Tests that valid properties do not generate warnings.
    /// </summary>
    [Fact]
    public void WFC0002_ValidProperty_NoWarning()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:TextBox ID="txtTest" Text="Hello" runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0002 = diagnostics.Where(d => d.Id == "WFC0002").ToList();
        Assert.Empty(wfc0002);
    }

    #endregion

    #region WFC0006 - UnexpectedClosingTag

    /// <summary>
    /// WFC0006: Tests that mismatched closing tags generate a warning.
    /// </summary>
    [Fact]
    public void WFC0006_UnexpectedClosingTag()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:Panel runat="server">
                    <div>Content</div>
                </wfc:Label>
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0006 = diagnostics.Where(d => d.Id == "WFC0006").ToList();
        Assert.Single(wfc0006);
        Assert.Equal(DiagnosticSeverity.Warning, wfc0006[0].Severity);
        Assert.Contains("Panel", wfc0006[0].GetMessage());
        Assert.Contains("Label", wfc0006[0].GetMessage());
    }

    /// <summary>
    /// WFC0006: Tests that properly matched tags do not generate warnings.
    /// </summary>
    [Fact]
    public void WFC0006_MatchedTags_NoWarning()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:Panel runat="server">
                    <div>Content</div>
                </wfc:Panel>
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0006 = diagnostics.Where(d => d.Id == "WFC0006").ToList();
        Assert.Empty(wfc0006);
    }

    #endregion

    #region WFC0007 - TypeNotFoundInNamespace

    /// <summary>
    /// WFC0007: Tests that a non-existent type in a registered namespace generates a warning.
    /// </summary>
    [Fact]
    public void WFC0007_TypeNotFoundInNamespace()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:NonExistentControl runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0007 = diagnostics.Where(d => d.Id == "WFC0007").ToList();
        Assert.Single(wfc0007);
        Assert.Equal(DiagnosticSeverity.Warning, wfc0007[0].Severity);
        Assert.Contains("NonExistentControl", wfc0007[0].GetMessage());
        // The message shows the tag prefix, not the full namespace
        Assert.Contains("wfc", wfc0007[0].GetMessage());
    }

    /// <summary>
    /// WFC0007: Tests that existing types do not generate warnings.
    /// </summary>
    [Fact]
    public void WFC0007_ExistingType_NoWarning()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:TextBox runat="server" ID="txtTest" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0007 = diagnostics.Where(d => d.Id == "WFC0007").ToList();
        Assert.Empty(wfc0007);
    }

    /// <summary>
    /// WFC0007: Tests type lookup with multiple registered namespaces.
    /// </summary>
    [Fact]
    public void WFC0007_MultipleNamespaces_TypeNotFound()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.HtmlControls" %>

                <wfc:CompletelyFakeControl runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0007 = diagnostics.Where(d => d.Id == "WFC0007").ToList();
        Assert.Single(wfc0007);
        Assert.Contains("CompletelyFakeControl", wfc0007[0].GetMessage());
    }

    #endregion

    #region Multiple Diagnostics

    /// <summary>
    /// Tests that multiple diagnostics of the same type can be generated from a single file.
    /// Note: WebControl and its derivatives implement IAttributeAccessor, so unknown attributes
    /// are allowed. We use Control which doesn't implement IAttributeAccessor.
    /// </summary>
    [Fact]
    public void MultipleDiagnostics_InSingleFile()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI" %>

                <wfc:Control InvalidProp1="Test" runat="server" />
                <wfc:Control InvalidProp2="Test" runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Should have multiple WFC0002 diagnostics for InvalidProp1 and InvalidProp2
        var wfc0002 = diagnostics.Where(d => d.Id == "WFC0002").ToList();
        Assert.Equal(2, wfc0002.Count);
        Assert.Contains(wfc0002, d => d.GetMessage().Contains("InvalidProp1"));
        Assert.Contains(wfc0002, d => d.GetMessage().Contains("InvalidProp2"));
    }

    /// <summary>
    /// Tests that WFC0002 and WFC0007 can appear together.
    /// </summary>
    [Fact]
    public void MultipleDiagnostics_DifferentTypes()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:FakeControl runat="server" />
                <wfc:Control InvalidProp="Test" runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Should have WFC0007 for FakeControl
        Assert.Contains(diagnostics, d => d.Id == "WFC0007" && d.GetMessage().Contains("FakeControl"));
        // Should have WFC0002 for InvalidProp
        Assert.Contains(diagnostics, d => d.Id == "WFC0002" && d.GetMessage().Contains("InvalidProp"));
    }

    /// <summary>
    /// Tests that controls implementing IAttributeAccessor allow unknown attributes.
    /// WebControl and its derivatives implement IAttributeAccessor.
    /// </summary>
    [Fact]
    public void WFC0002_IAttributeAccessor_AllowsUnknownAttributes()
    {
        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(
                "Example.ascx",
                """
                <%@ Control language="C#" %>
                <%@ Register TagPrefix="wfc" Namespace="WebFormsCore.UI.WebControls" %>

                <wfc:TextBox data-custom="value" aria-label="test" runat="server" />
                """
            )
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // TextBox extends WebControl which implements IAttributeAccessor
        // So unknown attributes should not generate WFC0002
        var wfc0002 = diagnostics.Where(d => d.Id == "WFC0002").ToList();
        Assert.Empty(wfc0002);
    }

    #endregion
}
