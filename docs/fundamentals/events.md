# Event Handling

WebForms Core uses an event-based model similar to classic WebForms, but modernized for asynchronous execution.

## Declaring Events in Markup

You can hook up events directly in your `.aspx` or `.ascx` files:

```aspx
<asp:Button ID="btnSubmit" runat="server" Text="Submit" OnClick="btnSubmit_Click" />
```

## Implementing Event Handlers

Event handlers in your code-behind can return `Task` (recommended for asynchronous work) or `void` (for synchronous logic).

```csharp
protected async Task btnSubmit_Click(object? sender, EventArgs e)
{
    // Perform async processing
    await Task.Delay(100); 
    litStatus.Text = "Form submitted successfully!";
}
```

For synchronous handlers, you can return `void`:

```csharp
protected void btnSubmit_Click(object? sender, EventArgs e)
{
    // Perform synchronous processing
    litStatus.Text = "Form submitted successfully!";
}
```

> **Note:** Some IDEs may incorrectly flag the handler return type. This is expected, because the source generator wraps handlers at compile time.

## Custom Events

You can create custom events in your User Controls and fire them from child controls.

```csharp
public partial class MyControl : Control
{
    public event AsyncEventHandler? ItemClicked;

    protected async Task OnItemClicked()
    {
        if (ItemClicked != null)
        {
            await ItemClicked.InvokeAsync(this, EventArgs.Empty);
        }
    }
}
```
---
**Note:** `AsyncEventHandler` is a special delegate type provided by WebForms Core that returns `Task`. The `InvokeAsync` extension method returns `ValueTask` for efficiency.
