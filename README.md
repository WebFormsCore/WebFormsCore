## What is WebForms Core?
WebForms Core is a framework for ASP.NET Core and ASP.NET.

It is heavily inspired by WebForms but is not a direct port. There are many breaking changes. The goal is to provide a framework that is easy to use and provides a familiar experience for developers who are used to WebForms.

> **Note:** This project is still in early development and is not ready for production use.

## Changes
In comparison to WebForms, there are a few changes:

- **Targets .NET Framework 4.7.2 and NET 6.0**  
  You can use WebForms Core on .NET (ASP.NET Core) and .NET Framework (ASP.NET and OWIN) 🎉
- **NativeAOT support**  
  WebForms Core supports Native AOT compilation for .NET 8.0 (preview 2 or higher).
- **Rendering is asynchronous**  
  By default, ASP.NET Core does not allow synchronous operations. This is done [to prevent thread starvation and app hangs](https://makolyte.com/aspnet-invalidoperationexception-synchronous-operations-are-disallowed/).
- **Designer source generators**  
  WebForms Core uses source generators to generate the fields for controls with an `ID`.
- **ViewState source generator**  
  In addition to using the [StateBag](https://learn.microsoft.com/en-us/dotnet/api/system.web.ui.statebag), you can use the attribute `[ViewState]` on properties and fields to store them in the view state.  
- **Multiple forms**  
  WebForms Core supports multiple forms that have their own view state on a single page.
- **Pre-compiled views**  
  WebForms Core pre-compiles views to improve the startup time of your application.
- **Content Security Policy (CSP) support**  
  Experimental support for Content Security Policy.

## Getting started
### ASP.NET Core (.NET 6.0)
Create a new .csproj that uses the SDK `WebFormsCore.SDK`:

```xml
<Project Sdk="WebFormsCore.SDK/0.0.1-alpha.10">

    <PropertyGroup>
        <TargetFramework>net6.0</TargetFramework>
    </PropertyGroup>

</Project>
```

In Program.cs, add WebFormsCore to the services and application builder:

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWebForms();

var app = builder.Build();

// Map '/' to 'Default.aspx'
app.MapAspx("/", "Default.aspx");

// Map all .aspx files
// For example, if you have a file named 'About.aspx' you can access it by going to '/About.aspx'
app.MapFallbackToAspx();

app.Run();
```

### ASP.NET (.NET Framework)
> **Note:** [Rider and Visual Studio 2022 17.5 (17.4 and 17.6 or higher are **supported**) does not support debugging .NET Framework applications with the new project system.](https://github.com/CZEMacLeod/MSBuild.SDK.SystemWeb/issues/51#issuecomment-1444781463)

Create a new .csproj that uses the SDK `WebFormsCore.SDK.NetFramework`:

```xml
<Project Sdk="WebFormsCore.SDK.NetFramework/0.0.1-alpha.10">

    <PropertyGroup>
        <TargetFramework>net472</TargetFramework>
        <LangVersion>10</LangVersion>
    </PropertyGroup>

</Project>
```

Add the following to your web.config:

> **Note:** This example will remove all the HTTP handlers that are registered by default so it does not conflict with .NET Framework WebForms.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <clear />
      <add verb="*" path="*.aspx" name="WebFormsCore-ASPX" type="WebFormsCore.PageHandlerFactory" />
    </handlers>
  </system.webServer>
</configuration>	
```	

Currently, it is not supported to map custom routes to .aspx files in ASP.NET.

### OWIN (.NET Framework)
> **Note:** [Rider and Visual Studio 2022 17.5 (17.4 and 17.6 or higher are **supported**) does not support debugging .NET Framework applications with the new project system.](https://github.com/CZEMacLeod/MSBuild.SDK.SystemWeb/issues/51#issuecomment-1444781463)

Create a new .csproj that uses the SDK `WebFormsCore.SDK.NetFramework` and add `<UseOwin>true</UseOwin>` to the PropertyGroup:

```xml
<Project Sdk="WebFormsCore.SDK.NetFramework/0.0.1-alpha.10">

    <PropertyGroup>
        <TargetFramework>net472</TargetFramework>
        <LangVersion>10</LangVersion>
        <UseOwin>true</UseOwin>
    </PropertyGroup>

</Project>
```

Create the file `Startup.cs` and add the following:

```cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using Application;

[assembly: OwinStartup(typeof(Startup))]

namespace Application
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var services = new ServiceCollection();

            services.UseOwinWebForms();

            app.Use<WebFormsCoreMiddleware>(services);
        }
    }
}
```

It is currently not supported to map custom routes to .aspx files in OWIN.

## Global controls
In the `web.config` you can register the controls that can be used without registering them in the page or control:

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
The runtime compiler is a feature that allows you to recompile the page (`.aspx`) and controls (`.ascx`) at runtime.
As of alpha.11, the runtime compiler is not included in the SDK. This is to reduce the size of the Native AOT binaries and for security reasons.

To add the runtime compiler to your project, add the following to your .csproj:

```xml
<PropertyGroup>
    <WebFormsCoreUseCompiler>true</WebFormsCoreUseCompiler>
</PropertyGroup>
```

and add the following to your `Program.cs`:

```cs
builder.Services.AddWebFormsCompiler();
```