using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.SourceGenerator;
using WebFormsCore.SourceGenerator.Tests.Utils;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class EndPointDirectiveTests
{
    [SkippableFact]
    public void Designer_EmitsAssemblyEndPointAttribute()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Line endings are different");

        var generator = new CSharpDesignGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore.UI;

            namespace Tests
            {
                public partial class EndPointPage : Page
                {
                }
            }
            """
        );

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "EndPointPage.aspx",
                    """
                    <%@ Page language="C#" Inherits="Tests.EndPointPage" EndPoint="/edit/{Id:int}" %>
                    <html>
                    <head runat="server"><title></title></head>
                    <body>
                        <form runat="server"></form>
                    </body>
                    </html>
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
        var runResult = driver.GetRunResult();

        var allGeneratedText = string.Join(Environment.NewLine,
            runResult.Results.Single().GeneratedSources
                .Select(s => s.SourceText.ToString()));

        Assert.Contains("AssemblyEndPointAttribute", allGeneratedText);
        Assert.Contains(@"/edit/{Id:int}", allGeneratedText);
    }

    [SkippableFact]
    public void Designer_OmitsAssemblyEndPointAttribute_WhenNoEndPoint()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Line endings are different");

        var generator = new CSharpDesignGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(Control).Assembly.Location)
        };

        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore.UI;

            namespace Tests
            {
                public partial class NormalPage : Page
                {
                }
            }
            """
        );

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { syntaxTree },
            references: references);

        driver = driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(
                new MemoryAdditionalText(
                    "NormalPage.aspx",
                    """
                    <%@ Page language="C#" Inherits="Tests.NormalPage" %>
                    <html>
                    <head runat="server"><title></title></head>
                    <body>
                        <form runat="server"></form>
                    </body>
                    </html>
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
        var runResult = driver.GetRunResult();

        var allGeneratedText = string.Join(Environment.NewLine,
            runResult.Results.Single().GeneratedSources
                .Select(s => s.SourceText.ToString()));

        Assert.Contains("AssemblyViewAttribute", allGeneratedText);
        Assert.DoesNotContain("AssemblyEndPointAttribute", allGeneratedText);
    }
}
