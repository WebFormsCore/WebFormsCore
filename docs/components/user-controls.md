# User and Custom Controls

WebForms Core supports two ways to build reusable UI components:

- **User Controls (`.ascx`)** for markup-driven reusable UI with code-behind.
- **Custom Controls (C# classes)** for fully code-based reusable controls.

## User Controls (.ascx)

User Controls allow you to encapsulate a piece of UI and logic into a reusable component.

### Creating a User Control

A User Control consists of a `.ascx` file and a code-behind `.ascx.cs` file.

**MyControl.ascx:**
```aspx
<%@ Control Language="C#" CodeBehind="MyControl.ascx.cs" Inherits="Example.MyControl" %>
<p>
    Hello from the User Control!
    <asp:Literal ID="litName" runat="server" />
</p>
```

**MyControl.ascx.cs:**
```csharp
namespace Example;

public partial class MyControl : WebFormsCore.UI.Control
{
    [ViewState] public string? Name { get; set; }

    protected override async ValueTask OnPreRenderAsync(CancellationToken token)
    {
        await base.OnPreRenderAsync(token);
        litName.Text = Name;
    }
}
```

### Registering and Using a User Control

You can register a user control in a Page or another User Control using the `@Register` directive.

```aspx
<%@ Page ... %>
<%@ Register TagPrefix="uc" TagName="MyControl" Src="~/Controls/MyControl.ascx" %>

<form runat="server">
    <uc:MyControl ID="ctrl1" runat="server" Name="John Doe" />
</form>
```

#### Controls from External Assemblies

To use controls defined in a separate assembly (e.g., a shared control library), use the `Assembly` and `Namespace` attributes instead of `Src`:

```aspx
<%@ Register TagPrefix="lib" Assembly="MyControls" Namespace="MyControls.Shared" %>

<lib:FancyWidget ID="widget1" runat="server" />
```

### Properties and ViewState

Use the `[ViewState]` attribute on properties you want to be able to set from the markup. The Source Generator will ensure these properties are correctly populated when the control is instantiated.

## Custom Controls (C# classes)

Beyond User Controls (`.ascx`), you can create fully reusable custom controls in C#.

### Basic Structure

A custom control inherits from `Control` (or `WebControl` for HTML-like properties).

```csharp
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

public class MyCustomLink : WebControl
{
    protected override HtmlTextWriterTag TagKey => HtmlTextWriterTag.A;

    [ViewState] public string? Url { get; set; }

    protected override async ValueTask AddAttributesToRender(HtmlTextWriter writer, CancellationToken token)
    {
        await base.AddAttributesToRender(writer, token);
        if (!string.IsNullOrEmpty(Url))
        {
            await writer.WriteAttributeAsync("href", Url);
        }
    }

    protected override async ValueTask RenderContentsAsync(HtmlTextWriter writer, CancellationToken token)
    {
        await writer.WriteAsync(ID);
    }
}
```

### Adding Properties

Attributes marked with `[ViewState]` are automatically persisted across postbacks.

### Overriding Render

The rendering logic in `WebControl` is split into several methods:
- `RenderAsync`: The main entry point.
- `RenderBeginTag`: Renders the opening tag.
- `AddAttributesToRender`: Adds attributes to the opening tag.
- `RenderContentsAsync`: Renders the content between tags.
- `RenderEndTagAsync`: Renders the closing tag.

Always use the asynchronous versions of the writer methods (e.g., `WriteAsync`, `WriteAttributeAsync`) and respect the `CancellationToken`.

### Using Custom Controls in Markup

To use a custom control in a `.aspx` or `.ascx` file, register it with the `@Register` directive specifying the namespace (and assembly when needed):

```aspx
<%@ Register TagPrefix="cc" Assembly="MyProject" Namespace="MyProject.Controls" %>

<form runat="server">
    <cc:MyCustomLink ID="link1" runat="server" Url="https://example.com" />
</form>
```
