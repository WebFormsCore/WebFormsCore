# App Startup

WebForms Core integrates seamlessly with the ASP.NET Core dependency injection and middleware pipeline.

## Basic Configuration

In your `Program.cs`, you need to register the WebForms Core services and middleware.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddWebFormsCore();

var app = builder.Build();

// Enable the middleware
app.UseWebFormsCore();

// Register page endpoints
app.MapPages();

app.Run();
```

### Builder Configuration

`AddWebFormsCore()` returns an `IWebFormsCoreBuilder` that you can use to add additional features:

```csharp
builder.Services.AddWebFormsCore()
    .AddSkeletonSupport();  // Enables skeleton rendering for lazy loading
```

Alternatively, you can pass a configuration action:

```csharp
builder.Services.AddWebFormsCore(wfc =>
{
    wfc.AddSkeletonSupport();
});
```

## Dependency Injection

WebForms Core fully supports dependency injection in your code-behind files. You can use constructor injection in your Pages and User Controls.

```csharp
public partial class MyPage(IMyService myService) : Page
{
    protected override async ValueTask OnLoadAsync(CancellationToken token)
    {
        await base.OnLoadAsync(token);
        var data = await myService.GetDataAsync();
    }
}
```

## Options Configuration

You can configure global behavior using the `WebFormsCoreOptions` class.

```csharp
builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    // Enable security headers (CSP, etc.) — enabled by default
    options.EnableSecurityHeaders = true;

    // Allow StreamPanel controls for WebSocket-based streaming
    options.AllowStreamPanel = true;
});
```

### Per-Page Configuration

Some settings are configured on individual pages rather than globally. For example, CSP and Early Hints are page-level features.

These settings can also be configured in the page tag, for example:

```aspx
<%@ Page Language="C#" EnableEarlyHints="true" %>
```

For full security guidance and recommended CSP/Early Hints setup, see [Security](../advanced/security.md).