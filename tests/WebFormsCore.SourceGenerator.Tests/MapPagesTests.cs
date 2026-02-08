using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore;
using WebFormsCore.UI;

// Assembly-level attribute applied at compile time for testing
[assembly: AssemblyRouteAttribute("/test/{Id:int}", typeof(WebFormsCore.SourceGenerator.Tests.MapPagesTestPage))]

namespace WebFormsCore.SourceGenerator.Tests;

public class MapPagesTestPage : Page
{
}

public class MapPagesTests
{
    [Fact]
    public void AssemblyRouteAttribute_IsAppliedCorrectly()
    {
        var assembly = typeof(MapPagesTestPage).Assembly;
        var attributes = assembly.GetCustomAttributes<AssemblyRouteAttribute>().ToList();

        Assert.NotEmpty(attributes);
        var attr = attributes.Single(a => a.PageType == typeof(MapPagesTestPage));
        Assert.Equal("/test/{Id:int}", attr.Pattern);
        Assert.Equal(typeof(MapPagesTestPage), attr.PageType);
    }

    [Fact]
    public void MapPagesFromAssembly_RegistersEndpoints()
    {
        var builder = WebApplication.CreateSlimBuilder(Array.Empty<string>());
        builder.Services.AddWebFormsCore();
        var app = builder.Build();

        var dataSourceCountBefore = ((IEndpointRouteBuilder)app).DataSources.Count;

        AspNetCoreExtensions.MapPagesFromAssembly(app, typeof(MapPagesTestPage).Assembly);

        var dataSourceCountAfter = ((IEndpointRouteBuilder)app).DataSources.Count;

        // MapPagesFromAssembly should have added at least one data source
        Assert.True(dataSourceCountAfter > dataSourceCountBefore,
            $"Expected data sources to increase. Before: {dataSourceCountBefore}, After: {dataSourceCountAfter}");
    }

    [Fact]
    public void MapPagesFromAssembly_SkipsAssemblyWithNoAttributes()
    {
        var builder = WebApplication.CreateSlimBuilder(Array.Empty<string>());
        builder.Services.AddWebFormsCore();
        var app = builder.Build();

        var dataSourceCountBefore = ((IEndpointRouteBuilder)app).DataSources.Count;

        // mscorlib/System.Runtime has no AssemblyRouteAttributes
        AspNetCoreExtensions.MapPagesFromAssembly(app, typeof(object).Assembly);

        var dataSourceCountAfter = ((IEndpointRouteBuilder)app).DataSources.Count;

        Assert.Equal(dataSourceCountBefore, dataSourceCountAfter);
    }
}
