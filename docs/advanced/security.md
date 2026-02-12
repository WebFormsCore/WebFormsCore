# Security in WebForms Core

WebForms Core includes experimental support for modern security features.

## Content Security Policy (CSP)

Content Security Policy is an added layer of security that helps to detect and mitigate certain types of attacks, including Cross-Site Scripting (XSS) and data injection attacks.

### Enabling CSP

First, ensure security headers are enabled globally in `Program.cs` (this is the default):

```csharp
builder.Services.Configure<WebFormsCoreOptions>(options =>
{
    options.EnableSecurityHeaders = true; // Enabled by default
});
```

Then configure CSP on individual pages. CSP is a **per-page** setting because each page may have different script and style requirements:

```csharp
protected override async ValueTask OnInitAsync(CancellationToken token)
{
    await base.OnInitAsync(token);
    Csp.Enabled = true;
    Csp.DefaultMode = CspMode.Nonce;
}
```

WebForms Core will automatically generate and inject `nonce` attributes into your scripts and styles to comply with a strict CSP.

## Early Hints

Early Hints (HTTP status code 103) allow the server to inform the browser about related resources (like stylesheets or scripts) *before* the main response is ready.

### Enabling Early Hints

You can enable Early Hints on a per-page basis:

```csharp
protected override async ValueTask OnInitAsync(CancellationToken token)
{
    await base.OnInitAsync(token);
    EnableEarlyHints = true;
}
```

When enabled, WebForms Core will send 103 Early Hints for any static assets required by the controls on the page, allowing the browser to start downloading them while the server is still processing the .aspx file.
