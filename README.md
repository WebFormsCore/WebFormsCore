## What is WebForms Core?
WebForms Core is a framework for ASP.NET Core 10.0 or higher.

It is heavily inspired by WebForms but is not a direct port. There are many breaking changes. The goal is to provide a framework that is easy to use and offers a familiar experience for developers accustomed to WebForms.

> **Note:** This project is still in early development and is not ready for production use.

## Changes
Compared to WebForms, there are several changes:

- **Native AOT support**  
  WebForms Core supports Native AOT compilation.
- **Rendering is asynchronous**  
  By default, ASP.NET Core does not allow synchronous operations. This is done [to prevent thread starvation and application hangs](https://makolyte.com/aspnet-invalidoperationexception-synchronous-operations-are-disallowed/).
- **Designer source generators**  
  WebForms Core uses source generators to generate fields for controls with an `ID`.
- **ViewState source generator**  
  In addition to using the [StateBag](https://learn.microsoft.com/en-us/dotnet/api/system.web.ui.statebag), you can use the `[ViewState]` attribute on properties and fields to store them in the view state.  
- **Multiple forms**  
  WebForms Core supports multiple forms, each with its own view state, on a single page.
- **Scoped forms**
  WebForms Core supports scoped forms. When a postback occurs in a scoped form, only the controls in that form are processed, and the view state of that form is loaded. This allows you to have multiple forms on a page for better performance.
- **Pre-compiled views**  
  WebForms Core pre-compiles views to improve the application's startup time.
- **Content Security Policy (CSP) support**  
  Experimental support for [Content Security Policy](https://developer.chrome.com/docs/privacy-security/csp).
- **Early Hints support**  
  Experimental support for [Early Hints](https://developer.chrome.com/docs/web-platform/early-hints).
- **Streaming support**  
  Similar to Blazor Server-Side, it is possible to stream HTML (without ViewState) using WebSockets.
- **Experimental code-only UI**  
  In addition to the traditional WebForms way of creating UI with `.aspx` and `.ascx` files, WebForms Core also supports creating UI with code only.
- **Lazy loading**  
  WebForms Core supports lazy loading within a page. This allows you to load parts of the page on demand, improving the initial load time of the page.

## Installation
Create a new `.csproj` file that uses the SDK `WebFormsCore.SDK`:

```xml
<Project Sdk="WebFormsCore.SDK.AspNetCore/0.0.1-alpha.80">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>

</Project>
```

In `Program.cs`, add WebFormsCore to the services and application builder:

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms();

var app = builder.Build();

// Registers static files middleware
app.UseWebFormsCore();

// Registers all pages (from all assemblies) as endpoints.
// You can also register them manually with `app.MapPage<T>()` or `app.MapPage("/path", typeof(Page))`
app.MapPages();

app.Run();
```

Create a new page by adding a `.aspx` file to the project:

```aspx
<%@ Page Language="C#" Route="/" CodeBehind="Default.aspx.cs" Inherits="Example.Default" %>
```

You can configure the route of the page with the `Route` attribute. If you don't specify a route, it will not be accessible with `app.MapPages()`.

## Code-only UI
It is also possible to map a control:

```cs
app.MapControl("/counter", context =>
{
    var counter = new Ref<int>();

    return new Panel
    {
        Controls =
        [
            new Panel
            {
                Controls = 
                [ 
                    Text(() => $"Counter: {counter.Value}")
                ],
                Style =
                {
                    ["margin-bottom"] = "10px"
                }
            },
            new Button
            {
                Text = "Increment",
                OnClick = (sender, args) => counter.Value++
            }
        ]
    };
});
```

## Global controls
In the `web.config`, you can register controls that can be used without explicitly registering them in the page or control:

```xml
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
```

## Runtime Compiler
The runtime compiler is a feature that allows you to recompile pages (`.aspx`) and controls (`.ascx`) at runtime.
As of alpha.13, the runtime compiler is not included in the SDK. This is to reduce the size of the Native AOT binaries and for security reasons.

To add the runtime compiler to your project, include the following in your `.csproj` file:

```xml
<PropertyGroup>
    <WebFormsCoreUseCompiler>true</WebFormsCoreUseCompiler>
</PropertyGroup>
```

And add the following to your `Program.cs`:

```cs
builder.Services.AddWebFormsCompiler();
```
