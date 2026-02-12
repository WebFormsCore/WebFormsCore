# Streaming and WebSockets

WebForms Core supports streaming updates to the browser using WebSockets, similar to Blazor Server.

## Introduction

Streaming allows you to update parts of the page without a full postback or even a traditional AJAX request. It is particularly useful for long-running operations or real-time data updates.

## Using StreamPanel

The `StreamPanel` control is the primary way to enable streaming. It acts as a container for other controls.

```aspx
<wfc:StreamPanel ID="myStream" runat="server">
    <app:Clock id="clock" runat="server" />
</wfc:StreamPanel>
```

You can also add it programmatically:

```csharp
var panel = LoadControl<StreamPanel>();
await panel.Controls.AddAsync(new Clock());
await Controls.AddAsync(panel);
```

## How it works

When a `StreamPanel` is rendered, it can establish a WebSocket connection. Once connected, the server can push HTML updates to the panel by calling `StateHasChanged()`.

```csharp
myStream.StateHasChanged();
```

## Connected Event

You can perform logic when a stream is established:

```csharp
protected override async ValueTask OnInitAsync(CancellationToken token)
{
    await base.OnInitAsync(token);
    myStream.Connected += OnConnected;
}

private async Task OnConnected(StreamPanel sender, EventArgs e)
{
    // Start background updates...
}
```

## Benefits

- **Low latency**: Updates are pushed instantly over an open socket.
- **Better UX**: No page reloads or "frozen" UI during updates.
- **No ViewState (Optional)**: Streaming can often be done without the overhead of ViewState if only one-way updates are needed.
