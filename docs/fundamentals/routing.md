# Routing

WebForms Core uses the ASP.NET Core routing system.

## Page Attribute

The most common way to define a route for a page is using the `Route` attribute in the `@Page` directive:

```aspx
<%@ Page Language="C#" Route="/my-page" CodeBehind="MyPage.aspx.cs" Inherits="Example.MyPage" %>
```

## Manual Mapping

If you prefer, you can register pages manually in `Program.cs` without using the `Route` attribute:

```csharp
app.MapPage<MyPage>("/custom-route");
// or
app.MapPage("/other-route", typeof(MyPage));
```

## Automatic Mapping

`app.MapPages()` will automatically scan for all classes (from all assemblies) inheriting from `WebFormsCore.UI.Page` that have the `Route` attribute and register them as endpoints.

## Parameters

Routes can include parameters just like standard ASP.NET Core routes:

```aspx
<%@ Page Language="C#" Route="/user/{id:int}" CodeBehind="User.aspx.cs" ... %>
```

You can then access these parameters in your code-behind using the `Context` object:

```csharp
var id = Request.RouteValues["id"];
```

Alternatively, you can define parameters as properties with the `[FromRoute]` attribute:

```csharp
[FromRoute] public int Id { get; set; }
```