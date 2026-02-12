# Master Pages

Master Pages provide a way to create a consistent layout for all pages in your application.

## Creating a Master Page

A Master Page uses the `.master` extension (or `.aspx` if configured as a master) and inherits from `MasterPage`.

**Site.master:**
```aspx
<%@ Master Language="C#" CodeBehind="Site.master.cs" Inherits="Example.SiteMaster" %>
<!DOCTYPE html>
<html>
<head>
    <title>My Site</title>
</head>
<body>
    <header><h1>Header</h1></header>
    
    <main>
        <asp:ContentPlaceHolder ID="MainContent" runat="server">
        </asp:ContentPlaceHolder>
    </main>

    <footer>Footer</footer>
</body>
</html>
```

## Using a Master Page

In your Page, specify the `MasterPageFile` in the `@Page` directive.

**Default.aspx:**
```aspx
<%@ Page Language="C#" MasterPageFile="~/Site.master" CodeBehind="Default.aspx.cs" ... %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2>Page Content</h2>
    <p>This content will be placed inside the Master Page's placeholder.</p>
</asp:Content>
```

## Nested Master Pages

WebForms Core supports nested master pages. A master page can itself have a `MasterPageFile` attribute.

## Master Page Code-Behind

Master Pages can have code-behind files just like regular pages:

**Site.master.cs:**
```csharp
namespace Example;

public partial class SiteMaster : WebFormsCore.UI.MasterPage
{
    [ViewState] public string? PageTitle { get; set; }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        await base.OnPreRenderAsync(token);
        // Custom logic for the master layout
    }
}
```

## Accessing the Master Page from a Content Page

You can access the master page instance from a content page via the `Master` property:

```csharp
protected override async ValueTask OnLoadAsync(CancellationToken token)
{
    await base.OnLoadAsync(token);
    if (Master is SiteMaster siteMaster)
    {
        siteMaster.PageTitle = "My Custom Title";
    }
}
```
