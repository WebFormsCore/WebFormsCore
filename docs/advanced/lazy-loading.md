# Lazy Loading

WebForms Core supports lazy-loading parts of a page to improve initial load performance.

## Introduction

Lazy loading allows you to defer the rendering and lifecycle of a control until it is actually needed, or until the initial page has already been sent to the browser.

## LazyLoader

The `LazyLoader` control is a specialized container that automatically triggers a postback after the initial page load to replace its skeleton content with real content. Its postback is isolated to the lazy-loading region, so it can load independently.

```aspx
<wfc:LazyLoader ID="lazyContent" OnContentLoaded="lazyContent_ContentLoaded" runat="server">
    <app:HeavyComponent ID="heavyComponent" runat="server" />
</wfc:LazyLoader>
```

### Events

You can handle the `ContentLoaded` event to perform initialization only when the lazy content is requested.

```csharp
protected async Task lazyContent_ContentLoaded(object sender, EventArgs e)
{
    // Perform any heavy initialization here. This code runs only when the lazy content is loaded.
    await heavyComponent.LoadDataAsync();
}
```

## SkeletonContainer

The `SkeletonContainer` is more manual. It renders skeleton placeholders when its `Loading` property is `true`.

```aspx
<wfc:SkeletonContainer runat="server" ID="skeleton" Loading="True">
    <div class="mb-3">
        <wfc:Label runat="server" ID="lblName" Text="Name:" />
        <wfc:TextBox runat="server" ID="txtName" />
    </div>
</wfc:SkeletonContainer>
```

When `Loading` is `True`, child controls like `TextBox` or `Label` will render as skeleton placeholders (if they support it, or if a `SkeletonRenderer` is registered for them).

## How it works

1.  **Skeleton Rendering**: Controls check if they are inside a container that is in "loading" state.
2.  **Specialized Renderers**: WebForms Core uses `ISkeletonRenderer` to decide how to render a control in skeleton mode.
3.  **Automatic Replacement**: `LazyLoader` uses client-side scripts to trigger a postback that sets `IsLoaded = true` and renders the real children.

## Setup

To use skeleton rendering with `SkeletonContainer`, you need to enable skeleton support in your `Program.cs`:

```csharp
builder.Services.AddWebFormsCore()
    .AddSkeletonSupport();
```

The `LazyLoader` control works without additional setup.

## Benefits

-   **Faster TTI (Time to Interactive)**: The browser can display the page structure immediately.
-   **Parallel Loading**: Multiple independent sections can load at once.
-   **Better for AOT**: Deferring heavy or dynamic logic can help keep the initial executable light.
