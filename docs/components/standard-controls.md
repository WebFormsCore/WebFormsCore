# Standard Controls

WebForms Core includes many of the common controls you would expect from classic WebForms.

## Basic Controls

### Literal

Renders text without any wrapping HTML element.

```aspx
<asp:Literal ID="litMessage" runat="server" Text="Hello World" />
```

```csharp
litMessage.Text = "Updated text";
```

### Label

Renders text inside a `<span>` element. Supports the `AssociatedControlID` property for accessibility.

```aspx
<asp:Label ID="lblName" runat="server" Text="Name:" AssociatedControlID="txtName" />
```

### TextBox

Renders an `<input type="text">` or `<textarea>` depending on the `TextMode` property.

```aspx
<asp:TextBox ID="txtName" runat="server" />
<asp:TextBox ID="txtBio" runat="server" TextMode="MultiLine" Rows="5" />
```

```csharp
var name = txtName.Text;
```

### Button

Renders a `<button>` element. Supports `Click` and `Command` events.

```aspx
<asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClick="btnSubmit_Click" />
```

```csharp
protected async Task btnSubmit_Click(object? sender, EventArgs e)
{
    // Handle click
}
```

### LinkButton

Renders an `<a>` element that triggers a postback, similar to `Button` but styled as a link.

```aspx
<asp:LinkButton ID="lnkAction" runat="server" Text="Click here" OnClick="lnkAction_Click" />
```

### CheckBox

Renders an `<input type="checkbox">`. Use the `Checked` property to read/set the state.

```aspx
<asp:CheckBox ID="chkAgree" runat="server" Text="I agree" />
```

```csharp
if (chkAgree.Checked)
{
    // ...
}
```

### DropDownList

Renders a `<select>` element. Add items using `ListItem` in markup or code.

```aspx
<asp:DropDownList ID="ddlColor" runat="server">
    <asp:ListItem Text="Red" Value="red" />
    <asp:ListItem Text="Blue" Value="blue" />
</asp:DropDownList>
```

```csharp
var selectedValue = ddlColor.SelectedValue;
```

## Container Controls

### Panel

Renders a `<div>` element. Useful as a grouping container.

```aspx
<asp:Panel ID="pnlContent" runat="server" CssClass="card">
    <p>Content goes here</p>
</asp:Panel>
```

### PlaceHolder

A container that doesn't render any HTML tag of its own. Useful for dynamically adding controls.

```aspx
<asp:PlaceHolder ID="phDynamic" runat="server" />
```

## Data Controls

### Repeater

Used to render a list of items based on a template. WebForms Core includes a generic `Repeater<T>` variant for type-safe data binding.

```aspx
<asp:Repeater ID="rptItems" runat="server">
    <ItemTemplate>
        <li><%# Container.DataItem %></li>
    </ItemTemplate>
</asp:Repeater>
```

```csharp
rptItems.DataSource = new[] { "Apple", "Banana", "Cherry" };
await rptItems.DataBindAsync();
```

## Navigation

### HyperLink

Renders an `<a>` tag.

```aspx
<asp:HyperLink ID="lnkHome" runat="server" NavigateUrl="/" Text="Home" />
```

## Validation Controls

- **RequiredFieldValidator**: Ensures a field is not empty.
- **CustomValidator**: Allows custom validation logic via server-side event handlers.

```aspx
<asp:TextBox ID="txtEmail" runat="server" />
<asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail" ErrorMessage="Email is required" />
```

---

## Common Properties

Most controls support these common properties:

| Property | Description |
|----------|-------------|
| `ID` | Unique identifier for the control |
| `runat="server"` | Required attribute to make a control server-side |
| `Visible` | Whether the control is rendered (`true`/`false`) |
| `CssClass` | CSS class(es) to apply to the rendered element |
| `Enabled` | Whether the control is enabled for user interaction |

> **Tip:** Attributes that are not recognized by the control are automatically rendered as HTML attributes. For example, `<asp:TextBox placeholder="Enter name" />` will render the `placeholder` attribute directly on the `<input>` element.
