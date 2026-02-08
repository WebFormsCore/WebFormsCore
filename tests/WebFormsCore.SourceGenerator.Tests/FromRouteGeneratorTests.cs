using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore;
using WebFormsCore.SourceGenerator;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class FromRouteGeneratorTests
{
    [Fact]
    public void Generator_EmitsPartialPropertyAndSetContext_ForIntProperty()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class EditPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("FromRoute"))
            .SourceText
            .ToString();

        // Verify backing field
        Assert.Contains("private int _fromRoute_Id;", generated);

        // Verify partial property implementation
        Assert.Contains("public partial int Id", generated);
        Assert.Contains("get => _fromRoute_Id;", generated);
        Assert.Contains("set => _fromRoute_Id = value;", generated);

        // Verify SetContext override
        Assert.Contains("protected internal override void SetContext(Microsoft.AspNetCore.Http.HttpContext context)", generated);
        Assert.Contains("base.SetContext(context);", generated);
        Assert.Contains("var routeValues = context.Request.RouteValues;", generated);

        // Verify route value binding with IAttributeParser pattern
        Assert.Contains("routeValues.TryGetValue(\"Id\"", generated);
        Assert.Contains("is int _typed_Id", generated);
        Assert.Contains("IAttributeParser<int>", generated);
        Assert.Contains(".Parse(", generated);
    }

    [Fact]
    public void Generator_EmitsStringConversion()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class SlugPage : Page
            {
                [FromRoute]
                public partial string Slug { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("FromRoute"))
            .SourceText
            .ToString();

        // String backing field has default! initializer
        Assert.Contains("private string _fromRoute_Slug = default!;", generated);

        // String conversion uses ToString
        Assert.Contains("routeValues.TryGetValue(\"Slug\"", generated);
        Assert.Contains("?.ToString() ?? default!;", generated);
    }

    [Fact]
    public void Generator_UsesCustomRouteName()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class CustomPage : Page
            {
                [FromRoute("slug")]
                public partial string Title { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("FromRoute"))
            .SourceText
            .ToString();

        // Should use custom route name "slug" instead of property name "Title"
        Assert.Contains("routeValues.TryGetValue(\"slug\"", generated);
        Assert.DoesNotContain("routeValues.TryGetValue(\"Title\"", generated);
    }

    [Fact]
    public void Generator_HandlesMultipleProperties()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class MultiPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }

                [FromRoute("slug")]
                public partial string Slug { get; set; }

                [FromRoute]
                public partial bool Active { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("FromRoute"))
            .SourceText
            .ToString();

        // All three properties should be generated
        Assert.Contains("_fromRoute_Id", generated);
        Assert.Contains("_fromRoute_Slug", generated);
        Assert.Contains("_fromRoute_Active", generated);

        // All three route value lookups
        Assert.Contains("routeValues.TryGetValue(\"Id\"", generated);
        Assert.Contains("routeValues.TryGetValue(\"slug\"", generated);
        Assert.Contains("routeValues.TryGetValue(\"Active\"", generated);

        // Bool uses IAttributeParser pattern
        Assert.Contains("is bool _typed_Active", generated);
        Assert.Contains("IAttributeParser<bool>", generated);
    }

    [Fact]
    public void Generator_SkipsNonPartialProperties()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class NonPartialPage : Page
            {
                [FromRoute]
                public int Id { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        // No sources should be generated since the property is not partial
        Assert.Empty(runResult.Results.Single().GeneratedSources);
    }

    [Fact]
    public void Generator_ProducesOutput_OnSecondRun()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class IncrPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatorResult = runResult.Results.Single();

        Assert.NotEmpty(generatorResult.GeneratedSources);
        Assert.Contains("_fromRoute_Id", generatorResult.GeneratedSources.Single().SourceText.ToString());
    }

    [Fact]
    public void Generator_HandlesGuidType()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using System;
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class GuidPage : Page
            {
                [FromRoute]
                public partial Guid Token { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var generated = runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("FromRoute"))
            .SourceText
            .ToString();

        Assert.Contains("routeValues.TryGetValue(\"Token\"", generated);
        Assert.Contains("is global::System.Guid _typed_Token", generated);
        Assert.Contains("IAttributeParser<global::System.Guid>", generated);
        Assert.Contains(".Parse(", generated);
    }

    [Fact]
    public void Generator_EmitsCorrectNamespace()
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(
            """
            using WebFormsCore;
            using WebFormsCore.UI;

            namespace My.Deep.Namespace;

            public partial class DeepPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }
            }
            """,
            parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new FromRouteGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var source = runResult.Results.Single().GeneratedSources.Single();

        Assert.Equal("My.Deep.Namespace.DeepPage.FromRoute.g.cs", source.HintName);

        var generated = source.SourceText.ToString();
        Assert.Contains("namespace My.Deep.Namespace", generated);
        Assert.Contains("partial class DeepPage", generated);
    }

    private static CSharpCompilation CreateCompilation(params SyntaxTree[] syntaxTrees)
    {
        return CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: syntaxTrees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        var assemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
            {
                assemblies.Add(path);
            }
        }

        assemblies.Add(typeof(Control).Assembly.Location);
        assemblies.Add(typeof(FromRouteAttribute).Assembly.Location);

        return assemblies
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();
    }

    private static GeneratorDriver CreateDriver(IIncrementalGenerator generator, CSharpParseOptions parseOptions, bool trackSteps = false)
    {
        var options = new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackSteps);

        return CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(generator.AsSourceGenerator()),
            parseOptions: parseOptions,
            driverOptions: options);
    }
}
