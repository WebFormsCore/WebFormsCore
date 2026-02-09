using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WebFormsCore.SourceGenerator;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

public class BindingSourceGeneratorTests
{
    // ==============================
    // FromRoute tests
    // ==============================

    [Fact]
    public void Generator_EmitsPartialPropertyAndSetContext_ForIntProperty()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class EditPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }
            }
            """);

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
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class SlugPage : Page
            {
                [FromRoute]
                public partial string Slug { get; set; }
            }
            """);

        // String backing field has default! initializer
        Assert.Contains("private string _fromRoute_Slug = default!;", generated);

        // String conversion uses ToString
        Assert.Contains("routeValues.TryGetValue(\"Slug\"", generated);
        Assert.Contains("?.ToString() ?? default!;", generated);
    }

    [Fact]
    public void Generator_UsesCustomRouteName()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class CustomPage : Page
            {
                [FromRoute(Name = "slug")]
                public partial string Title { get; set; }
            }
            """);

        // Should use custom route name "slug" instead of property name "Title"
        Assert.Contains("routeValues.TryGetValue(\"slug\"", generated);
        Assert.DoesNotContain("routeValues.TryGetValue(\"Title\"", generated);
    }

    [Fact]
    public void Generator_HandlesMultipleProperties()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class MultiPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }

                [FromRoute(Name = "slug")]
                public partial string Slug { get; set; }

                [FromRoute]
                public partial bool Active { get; set; }
            }
            """);

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
            using Microsoft.AspNetCore.Mvc;
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
        var driver = CreateDriver(new BindingSourceGenerator(), parseOptions);

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
            using Microsoft.AspNetCore.Mvc;
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
        var driver = CreateDriver(new BindingSourceGenerator(), parseOptions);

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
        var generated = GenerateSource(
            """
            using System;
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class GuidPage : Page
            {
                [FromRoute]
                public partial Guid Token { get; set; }
            }
            """);

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
            using Microsoft.AspNetCore.Mvc;
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
        var driver = CreateDriver(new BindingSourceGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        var source = runResult.Results.Single().GeneratedSources.Single();

        Assert.Equal("My.Deep.Namespace.DeepPage.Binding.g.cs", source.HintName);

        var generated = source.SourceText.ToString();
        Assert.Contains("namespace My.Deep.Namespace", generated);
        Assert.Contains("partial class DeepPage", generated);
    }

    // ==============================
    // FromQuery tests
    // ==============================

    [Fact]
    public void Generator_EmitsQueryBinding_ForStringProperty()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class SearchPage : Page
            {
                [FromQuery]
                public partial string Search { get; set; }
            }
            """);

        // Verify backing field
        Assert.Contains("private string _fromQuery_Search = default!;", generated);

        // Verify partial property
        Assert.Contains("public partial string Search", generated);
        Assert.Contains("get => _fromQuery_Search;", generated);
        Assert.Contains("set => _fromQuery_Search = value;", generated);

        // Verify query binding
        Assert.Contains("context.Request.Query.TryGetValue(\"Search\"", generated);
        Assert.Contains("_qv_Search", generated);
        Assert.Contains("_qv_Search.Count > 0", generated);
        Assert.Contains("Search = _qv_Search.ToString();", generated);

        // Should NOT have routeValues since there are no route properties
        Assert.DoesNotContain("var routeValues", generated);
    }

    [Fact]
    public void Generator_EmitsQueryBinding_ForIntProperty()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class PaginatedPage : Page
            {
                [FromQuery]
                public partial int PageNumber { get; set; }
            }
            """);

        // Verify query binding with parser
        Assert.Contains("context.Request.Query.TryGetValue(\"PageNumber\"", generated);
        Assert.Contains("IAttributeParser<int>", generated);
        Assert.Contains(".Parse(_qv_PageNumber.ToString())", generated);
    }

    [Fact]
    public void Generator_EmitsQueryBinding_WithCustomName()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class FilterPage : Page
            {
                [FromQuery(Name = "q")]
                public partial string SearchTerm { get; set; }
            }
            """);

        // Should use "q" not "SearchTerm"
        Assert.Contains("context.Request.Query.TryGetValue(\"q\"", generated);
        Assert.DoesNotContain("TryGetValue(\"SearchTerm\"", generated);
    }

    // ==============================
    // FromHeader tests
    // ==============================

    [Fact]
    public void Generator_EmitsHeaderBinding_ForStringProperty()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class HeaderPage : Page
            {
                [FromHeader]
                public partial string Accept { get; set; }
            }
            """);

        // Verify backing field
        Assert.Contains("private string _fromHeader_Accept = default!;", generated);

        // Verify header binding
        Assert.Contains("context.Request.Headers.TryGetValue(\"Accept\"", generated);
        Assert.Contains("_hv_Accept", generated);
        Assert.Contains("_hv_Accept.Count > 0", generated);
        Assert.Contains("Accept = _hv_Accept.ToString();", generated);

        // Should NOT have routeValues
        Assert.DoesNotContain("var routeValues", generated);
    }

    [Fact]
    public void Generator_EmitsHeaderBinding_WithCustomName()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class CustomHeaderPage : Page
            {
                [FromHeader(Name = "X-Custom-Header")]
                public partial string CustomHeader { get; set; }
            }
            """);

        // Should use custom header name
        Assert.Contains("context.Request.Headers.TryGetValue(\"X-Custom-Header\"", generated);
        Assert.DoesNotContain("TryGetValue(\"CustomHeader\"", generated);
    }

    [Fact]
    public void Generator_EmitsHeaderBinding_ForIntProperty()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public partial class HeaderIntPage : Page
            {
                [FromHeader(Name = "X-Page-Size")]
                public partial int PageSize { get; set; }
            }
            """);

        // Verify header binding with parser
        Assert.Contains("context.Request.Headers.TryGetValue(\"X-Page-Size\"", generated);
        Assert.Contains("IAttributeParser<int>", generated);
        Assert.Contains(".Parse(_hv_PageSize.ToString())", generated);
    }

    // ==============================
    // FromServices tests
    // ==============================

    [Fact]
    public void Generator_EmitsServicesBinding_ForNonNullable()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public interface IMyService { }

            public partial class ServicePage : Page
            {
                [FromServices]
                public partial IMyService MyService { get; set; }
            }
            """);

        // Verify backing field (reference types other than string don't get = default!)
        Assert.Contains("_fromServices_MyService;", generated);

        // Verify service resolution with GetRequiredService
        Assert.Contains("MyService = context.RequestServices.GetRequiredService<global::Tests.IMyService>();", generated);

        // Should NOT have routeValues or query/header
        Assert.DoesNotContain("var routeValues", generated);
        Assert.DoesNotContain("context.Request.Query", generated);
        Assert.DoesNotContain("context.Request.Headers", generated);
    }

    [Fact]
    public void Generator_EmitsServicesBinding_ForNullable()
    {
        var generated = GenerateSource(
            """
            #nullable enable
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public interface IMyService { }

            public partial class NullableServicePage : Page
            {
                [FromServices]
                public partial IMyService? MyService { get; set; }
            }
            """);

        // Verify service resolution with GetService (nullable)
        Assert.Contains("global::Tests.IMyService? _fromServices_MyService;", generated);
        Assert.Contains("MyService = context.RequestServices.GetService<global::Tests.IMyService>();", generated);
        Assert.DoesNotContain("GetRequiredService", generated);
    }

    // ==============================
    // Mixed binding sources
    // ==============================

    [Fact]
    public void Generator_HandlesMixedBindingSources()
    {
        var generated = GenerateSource(
            """
            using Microsoft.AspNetCore.Mvc;
            using WebFormsCore.UI;

            namespace Tests;

            public interface IMyService { }

            public partial class MixedPage : Page
            {
                [FromRoute]
                public partial int Id { get; set; }

                [FromQuery]
                public partial string Search { get; set; }

                [FromHeader(Name = "X-Request-Id")]
                public partial string RequestId { get; set; }

                [FromServices]
                public partial IMyService MyService { get; set; }
            }
            """);

        // Verify all four binding types are present
        Assert.Contains("_fromRoute_Id", generated);
        Assert.Contains("_fromQuery_Search", generated);
        Assert.Contains("_fromHeader_RequestId", generated);
        Assert.Contains("_fromServices_MyService", generated);

        // Route binding
        Assert.Contains("var routeValues = context.Request.RouteValues;", generated);
        Assert.Contains("routeValues.TryGetValue(\"Id\"", generated);

        // Query binding
        Assert.Contains("context.Request.Query.TryGetValue(\"Search\"", generated);

        // Header binding
        Assert.Contains("context.Request.Headers.TryGetValue(\"X-Request-Id\"", generated);

        // Service binding
        Assert.Contains("context.RequestServices.GetRequiredService<global::Tests.IMyService>()", generated);
    }

    // ==============================
    // Helpers
    // ==============================

    private static string GenerateSource(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CreateCompilation(syntaxTree);
        var driver = CreateDriver(new BindingSourceGenerator(), parseOptions);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        return runResult.Results.Single().GeneratedSources
            .Single(s => s.HintName.Contains("Binding"))
            .SourceText
            .ToString();
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
