using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.SourceGenerator.Tests.Utils;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public sealed class IncludeDirectiveTest : IDisposable
{
    private readonly string _tempDir;

    public IncludeDirectiveTest()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"WebFormsCore_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    void IDisposable.Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private GeneratorDriver CreateDriver()
    {
        var generator = new CSharpDesignGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.WithUpdatedAnalyzerConfigOptions(
            new TestAnalyzerConfigOptionsProvider(
                new Dictionary<string, string>
                {
                    ["build_property.MSBuildProjectDirectory"] = _tempDir.Replace('\\', '/')
                }));

        return driver;
    }

    private static CSharpCompilation CreateCompilation()
    {
        return CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText("""
                    namespace Tests
                    {
                        public partial class DefaultPage : WebFormsCore.UI.Page { }
                    }
                    """)
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            references: [MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)]);
    }

    /// <summary>
    /// Tests that include directive works correctly by including content from another file.
    /// </summary>
    [Fact]
    public void IncludeDirective_IncludesContentFromFile()
    {
        // Create included file
        var includeDir = Path.Combine(_tempDir, "Common");
        Directory.CreateDirectory(includeDir);
        var includePath = Path.Combine(includeDir, "Include.ascx");
        File.WriteAllText(includePath, """
            <div>Hello from included file</div>
            """);

        // Create main page
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <html>
            <body>
            <!--#include file="Common/Include.ascx" -->
            </body>
            </html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Tests that when an unresolved control is referenced in an included file,
    /// the diagnostic correctly points to the included file path, not the main file.
    /// </summary>
    [Fact]
    public void IncludeDirective_UnresolvedControlInIncludedFile_ShowsCorrectPath()
    {
        // Create included file with unresolved control reference
        var includeDir = Path.Combine(_tempDir, "Common");
        Directory.CreateDirectory(includeDir);
        var includePath = Path.Combine(includeDir, "Include.ascx");
        File.WriteAllText(includePath, """
            <%@ Register TagPrefix="test" TagName="NotExists" Src="~/NotExisting/Control.ascx" %>
            <div>Hello from included file</div>
            """);

        // Create main page that includes the above file
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <html>
            <body>
            <!--#include file="Common/Include.ascx" -->
            </body>
            </html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Should have one WFC0004 warning
        var wfc0004 = diagnostics.Where(d => d.Id == "WFC0004").ToList();
        Assert.Single(wfc0004);

        var diagnostic = wfc0004[0];

        // The diagnostic location should point to the included file, not the main file
        Assert.Contains("Include.ascx", diagnostic.Location.GetLineSpan().Path);
        Assert.DoesNotContain("Default.aspx", diagnostic.Location.GetLineSpan().Path);

        // The message should contain the control path that couldn't be found
        var message = diagnostic.GetMessage();
        Assert.Contains("~/NotExisting/Control.ascx", message);
        Assert.DoesNotContain("Include.ascx", message); // Should not show the current file in the message
    }

    /// <summary>
    /// Tests that nested includes work correctly.
    /// </summary>
    [Fact]
    public void IncludeDirective_NestedIncludes_WorksCorrectly()
    {
        // Create nested include structure
        var commonDir = Path.Combine(_tempDir, "Common");
        var nestedDir = Path.Combine(commonDir, "Nested");
        Directory.CreateDirectory(nestedDir);

        // Deeply nested file
        var deepPath = Path.Combine(nestedDir, "Deep.ascx");
        File.WriteAllText(deepPath, "<span>Deep content</span>");

        // First level include that references nested file
        var includePath = Path.Combine(commonDir, "Include.ascx");
        File.WriteAllText(includePath, """
            <div>
            <!--#include file="Nested/Deep.ascx" -->
            </div>
            """);

        // Create main page
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <html>
            <body>
            <!--#include file="Common/Include.ascx" -->
            </body>
            </html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    #region WFC0003 - DuplicateControlRegister

    /// <summary>
    /// WFC0003: Tests that duplicate control registrations generate a warning.
    /// </summary>
    [Fact]
    public void WFC0003_DuplicateControlRegister()
    {
        // Create two control files
        var controlsDir = Path.Combine(_tempDir, "Controls");
        Directory.CreateDirectory(controlsDir);

        var control1Path = Path.Combine(controlsDir, "Test1.ascx");
        File.WriteAllText(control1Path, """
            <%@ Control language="C#" Inherits="WebFormsCore.UI.Control" %>
            <span>Control 1</span>
            """);

        var control2Path = Path.Combine(controlsDir, "Test2.ascx");
        File.WriteAllText(control2Path, """
            <%@ Control language="C#" Inherits="WebFormsCore.UI.Control" %>
            <span>Control 2</span>
            """);

        // Create main page with duplicate registrations
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <%@ Register TagPrefix="app" TagName="Test" Src="~/Controls/Test1.ascx" %>
            <%@ Register TagPrefix="app" TagName="Test" Src="~/Controls/Test2.ascx" %>
            <html><body></body></html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0003 = diagnostics.Where(d => d.Id == "WFC0003").ToList();
        Assert.Single(wfc0003);
        Assert.Contains("app", wfc0003[0].GetMessage());
        Assert.Contains("Test", wfc0003[0].GetMessage());
    }

    /// <summary>
    /// WFC0003: Tests that different tag names do not trigger duplicate warning.
    /// </summary>
    [Fact]
    public void WFC0003_DifferentTagNames_NoWarning()
    {
        // Create two control files
        var controlsDir = Path.Combine(_tempDir, "Controls");
        Directory.CreateDirectory(controlsDir);

        var control1Path = Path.Combine(controlsDir, "Test1.ascx");
        File.WriteAllText(control1Path, """
            <%@ Control language="C#" Inherits="WebFormsCore.UI.Control" %>
            <span>Control 1</span>
            """);

        var control2Path = Path.Combine(controlsDir, "Test2.ascx");
        File.WriteAllText(control2Path, """
            <%@ Control language="C#" Inherits="WebFormsCore.UI.Control" %>
            <span>Control 2</span>
            """);

        // Create main page with different tag names
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <%@ Register TagPrefix="app" TagName="Test1" Src="~/Controls/Test1.ascx" %>
            <%@ Register TagPrefix="app" TagName="Test2" Src="~/Controls/Test2.ascx" %>
            <html><body></body></html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0003 = diagnostics.Where(d => d.Id == "WFC0003").ToList();
        Assert.Empty(wfc0003);
    }

    #endregion

    #region WFC0004 - ControlNotFound

    /// <summary>
    /// WFC0004: Tests that a non-existent control file generates a warning.
    /// </summary>
    [Fact]
    public void WFC0004_ControlNotFound()
    {
        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <%@ Register TagPrefix="app" TagName="Missing" Src="~/Controls/DoesNotExist.ascx" %>
            <html><body></body></html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0004 = diagnostics.Where(d => d.Id == "WFC0004").ToList();
        Assert.Single(wfc0004);
        Assert.Contains("~/Controls/DoesNotExist.ascx", wfc0004[0].GetMessage());
    }

    #endregion

    #region WFC0005 - InheritNotFound

    /// <summary>
    /// WFC0005: Tests that a control file without Inherits attribute generates a warning.
    /// </summary>
    [Fact]
    public void WFC0005_InheritNotFound()
    {
        // Create a control file without Inherits attribute
        var controlsDir = Path.Combine(_tempDir, "Controls");
        Directory.CreateDirectory(controlsDir);

        var controlPath = Path.Combine(controlsDir, "NoInherit.ascx");
        File.WriteAllText(controlPath, """
            <%@ Control language="C#" %>
            <span>Control without Inherits</span>
            """);

        var mainPath = Path.Combine(_tempDir, "Default.aspx");
        var mainContent = """
            <%@ Page language="C#" Inherits="Tests.DefaultPage" %>
            <%@ Register TagPrefix="app" TagName="NoInherit" Src="~/Controls/NoInherit.ascx" %>
            <html><body></body></html>
            """;
        File.WriteAllText(mainPath, mainContent);

        var compilation = CreateCompilation();
        var driver = CreateDriver();

        driver = driver.AddAdditionalTexts([
            new MemoryAdditionalText(mainPath, mainContent)
        ]);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var wfc0005 = diagnostics.Where(d => d.Id == "WFC0005").ToList();
        Assert.Single(wfc0005);
        Assert.Contains("~/Controls/NoInherit.ascx", wfc0005[0].GetMessage());
    }

    #endregion
}
