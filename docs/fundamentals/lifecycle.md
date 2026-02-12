# Control Lifecycle

The control lifecycle in WebForms Core is similar to classic WebForms.

## Execution Order

Each control follows a strict set of lifecycle stages:

1.  **FrameworkInit**: Basic control setup.
2.  **PreInit**: The very first stage where the page is set up.
3.  **Init**: Controls are initialized and the control tree is built.
4.  **LoadViewState**: ViewState is loaded from the request (if any).
5.  **Load**: The main entry point for page logic.
6.  **ProcessPostData**: Controls process any incoming postback data.
7.  **RaiseChangedEvents**: Controls raise events if their data changed (e.g., `TextChanged`).
8.  **RaisePostBackEvent**: The primary event handler (e.g., `Click`) is executed.
9.  **PreRender**: Last-minute changes before rendering.
10. **SaveViewState**: Current state is saved to the ViewState buffer.
11. **Render**: HTML is written to the output stream.
12. **Unload**: Cleanup after the request is finished.

## Asynchronous Methods

Every lifecycle stage has a corresponding `ValueTask`-returning method. When overriding these, ensure you use the `Async` version and handle the `CancellationToken`:

```csharp
protected override async ValueTask OnLoadAsync(CancellationToken token)
{
    await base.OnLoadAsync(token);
    // Your async logic here
}
```

## Important Differences

- **Async by Default**: Unlike classic WebForms, lifecycle methods use async versions (`OnInitAsync`, `OnLoadAsync`, etc.). There are no synchronous versions like `OnInit` or `OnLoad`.
- **CancellationToken**: Every async lifecycle method receives a `CancellationToken` which should be respected, especially for long-running or I/O operations.
- **ViewState Exception**: `OnLoadViewState` and `OnWriteViewState` remain synchronous as they operate on memory buffers using `ref` reader/writer types.
