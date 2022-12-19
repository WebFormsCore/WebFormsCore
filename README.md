## What is WebForms Core?
WebForms Core is a framework for ASP.NET Core and ASP.NET.

It's heavily inspired by WebForms but is not a direct port. There are a lot of breaking changes. The goal is to provide a framework that is easy to use and provides a familiar experience for developers who are used to WebForms.

> **Note:** This project is still in early development and is not ready for production use.

## Changes
In comparison to WebForms there are a few changes:

- **Targets .NET Framework 4.7.2 and NET 6.0**  
  You can use WebForms Core on .NET and .NET Framework 🎉
- **Rendering is asynchronous**  
  By default, ASP.NET Core doesn't allow synchronous operations. This is done [to prevent thread starvation and app hangs](https://makolyte.com/aspnet-invalidoperationexception-synchronous-operations-are-disallowed/).
- **Designer source generators**  
  WebForms Core uses source generators to generate the fields for controls with an `ID`.
- **ViewState source generator**  
  In addition of using `ViewState` to store control state, you now use the attribute `[ViewState]` on properties and fields to store them in the view state.  
- **Multiple forms**  
  WebForms Core supports multiple forms that have their own view state on a single page.
- **Pre-compiled views**  
  WebForms Core pre-compiles views to improve the startup time of your application.
- **Content Security Policy (CSP) support**  
  Experimental support for Content Security Policy.

## Getting started
Create a new .csproj that targets the SDK of WebFormsCore:

```xml
<Project Sdk="WebFormsCore.SDK/0.0.1-alpha.6">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
    </PropertyGroup>

</Project>
```

In `Program.cs`, add WebFormsCore to the services and application builder:
```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms();

var app = builder.Build();

// Uncomment the following if you want to more compability: https://github.com/dotnet/systemweb-adapters/blob/main/docs/usage_guidance.md
// app.UseSystemWebAdapters();

// Map '/' to 'Default.aspx'
app.MapAspx("/", "Default.aspx");

// Map all .aspx files
app.MapFallbackToAspx();

app.Run();
```

_Optional:_ Create the file `web.config` with the control namespaces:

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

Every `.aspx` file in the project will now be handled.