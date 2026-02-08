using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore.SourceGenerator.Tests;

[Authorize]
public class AuthorizedPage : Page { }

[Authorize(Policy = "Admin")]
public class AdminPolicyPage : Page { }

[Authorize(Roles = "Manager")]
public class RoleAuthorizedPage : Page { }

[AllowAnonymous]
public class AnonymousPage : Page { }

public class NoAuthPage : Page { }

public class AuthorizationEndpointTests
{
    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateSlimBuilder(Array.Empty<string>());
        builder.Services.AddWebFormsCore();
        return builder.Build();
    }

    private static Endpoint GetLastEndpoint(WebApplication app)
    {
        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints)
            .Last();
    }

    [Fact]
    public void MapPage_WithAuthorizeAttribute_AddsAuthorizationMetadata()
    {
        var app = CreateApp();

        app.MapPage<AuthorizedPage>("/secure");

        var endpoint = GetLastEndpoint(app);
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.NotEmpty(authorizeData);
    }

    [Fact]
    public void MapPage_WithAuthorizePolicy_PreservesPolicy()
    {
        var app = CreateApp();

        app.MapPage<AdminPolicyPage>("/admin");

        var endpoint = GetLastEndpoint(app);
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Single(authorizeData);
        Assert.Equal("Admin", authorizeData[0].Policy);
    }

    [Fact]
    public void MapPage_WithAuthorizeRoles_PreservesRoles()
    {
        var app = CreateApp();

        app.MapPage<RoleAuthorizedPage>("/manager");

        var endpoint = GetLastEndpoint(app);
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Single(authorizeData);
        Assert.Equal("Manager", authorizeData[0].Roles);
    }

    [Fact]
    public void MapPage_WithAllowAnonymous_AddsAnonymousMetadata()
    {
        var app = CreateApp();

        app.MapPage<AnonymousPage>("/public");

        var endpoint = GetLastEndpoint(app);
        var anonymousMetadata = endpoint.Metadata.GetOrderedMetadata<IAllowAnonymous>();
        Assert.NotEmpty(anonymousMetadata);
    }

    [Fact]
    public void MapPage_WithoutAuthAttributes_HasNoAuthorizationMetadata()
    {
        var app = CreateApp();

        app.MapPage<NoAuthPage>("/plain");

        var endpoint = GetLastEndpoint(app);
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.Empty(authorizeData);
    }

    [Fact]
    public void MapPageByType_WithAuthorizeAttribute_AddsAuthorizationMetadata()
    {
        var app = CreateApp();

        app.MapPage("/secure", typeof(AuthorizedPage));

        var endpoint = GetLastEndpoint(app);
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        Assert.NotEmpty(authorizeData);
    }
}
