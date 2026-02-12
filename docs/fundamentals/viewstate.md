# ViewState Management

WebForms Core provides a familiar ViewState mechanism but with modern improvements using Source Generators.

## Traditional StateBag

You can still use the `ViewState` collection just like in classic WebForms:

```csharp
ViewState["MyKey"] = "Some Value";
var val = (string)ViewState["MyKey"];
```

## ViewState Attribute (Recommended)

WebForms Core introduces a `[ViewState]` attribute that allows you to mark properties or fields to be automatically persisted in the ViewState. This is powered by a Source Generator, making it efficient and type-safe.

```csharp
public partial class MyControl : Control
{
    [ViewState] public int Counter { get; set; }

    protected async Task Increment_Click(object? sender, EventArgs e)
    {
        Counter++;
    }
}
```

The source generator automatically handles the serialization and deserialization of the `Counter` property into the hidden ViewState field on the page.

## Multiple Forms and Scoped ViewState

Unlike classic WebForms, WebForms Core supports multiple `<form runat="server">` elements on a single page. Each form maintains its own independent ViewState.

When a postback occurs within a form, only the ViewState for that specific form is sent and processed, which can significantly improve performance for large pages.

## ViewState Configuration

You can configure ViewState globally using `ViewStateOptions`:

```csharp
builder.Services.Configure<ViewStateOptions>(options =>
{
    // Enable or disable ViewState globally
    options.Enabled = true;

    // Use compact serialization to reduce payload size
    options.Compact = true;

    // Set an encryption key for ViewState security
    options.EncryptionKey = "your-secret-key-here";

    // Limit maximum size to prevent abuse
    options.MaxBytes = 1_000_000;
});
```
