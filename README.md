## What is WebForms Core?
WebForms Core is a framework for ASP.NET Core and ASP.NET.

It's heavily inspired by WebForms but is not a direct port. There are a lot of breaking changes. The goal is to provide a framework that is easy to use and provides a familiar experience for developers who are used to WebForms.

> **Note:** This project is still in early development and is not ready for production use.

## Changes
In comparison to WebForms there are a few changes:

- **Targets .NET Framework 4.7.2 and NET 6.0**  
  You can use WebForms Core on .NET and .NET Framework 🎉
  
  > **Note:** The WebForms designer in Visual Studio (with ReSharper) and Rider gives an error that the page doesn't extend `System.Web.UI.Page`. This can be ignored.

  > **Note:** .NET Framework doesn't support Brotoli view state compression.
- **Rendering is asynchronous**  
  By default, ASP.NET Core doesn't allow synchronous operations. This is done [to prevent thread starvation and app hangs](https://makolyte.com/aspnet-invalidoperationexception-synchronous-operations-are-disallowed/).
- **Designer source generators**  
  WebForms Core uses source generators to generate the fields for controls with an `ID`.
- **ViewState source generator**  
  In addition of using `ViewState` to store control state, you now use the attribute `[ViewState]` on properties and fields to store them in the view state.  
- **Multiple forms**  
  WebForms Core supports multiple forms on a single page. This means that you can have multiple `form` elements on a page and each one will have its own view state.
