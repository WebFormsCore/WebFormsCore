using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IO;

namespace WebFormsCore.UI.WebControls;

[JsonSerializable(typeof(WebSocketCommand))]
[JsonSerializable(typeof(JavaScriptWebFormsCoreOptions))]
internal partial class JsonContext : JsonSerializerContext
{
}

internal record JavaScriptWebFormsCoreOptions(
    [property: JsonPropertyName("updateScripts")] bool RenderScriptOnPostBack,
    [property: JsonPropertyName("updateStyles")] bool RenderStyleOnPostBack
);

internal record WebSocketCommand(
    [property: JsonPropertyName("t"), Required] string EventTarget,
    [property: JsonPropertyName("a")] string? EventArgument
);

public class StreamPanel : Control, INamingContainer
{
    internal static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

    private WebSocket _webSocket = null!;
    private Task<WebSocketReceiveResult>? _receiveTask;
    private bool _prerender;
    private TaskCompletionSource _stateHasChangedTcs = new();
    private IOptions<WebFormsCoreOptions>? _options;

    public event AsyncEventHandler<StreamPanel, EventArgs>? Connected;

    public event AsyncEventHandler<StreamPanel, EventArgs>? Disconnected;

    public bool IsConnected { get; internal set; }

    public bool Prerender
    {
        get => _prerender;
        set
        {
            if (_state >= ControlState.Initialized)
            {
                throw new InvalidOperationException("Cannot change prerender after framework initialization.");
            }

            _prerender = value;
        }
    }

    protected override bool ProcessControl => Page.IsStreaming || _prerender;

    public override bool EnableViewState => false;

    public WebSocket WebSocket
    {
        get
        {
            if (_webSocket is null)
            {
                throw new InvalidOperationException();
            }

            return _webSocket;
        }
    }

    public override void StateHasChanged()
    {
        if (!IsConnected) return;

        _stateHasChangedTcs.TrySetResult();
    }

    internal async Task StartAsync(HttpContext context, WebSocket websocket)
    {
        context.Response.Body = Stream.Null;

        _webSocket = websocket;

        await Connected.InvokeAsync(this, EventArgs.Empty);
        await UpdateControlAsync();

        var incoming = new byte[1024]; // TODO: Use a buffer pool

        var status = WebSocketCloseStatus.NormalClosure;
        var message = "Done";

        var cancellationToken = context.RequestServices.GetService<IHostApplicationLifetime>()?.ApplicationStopping ?? default;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _receiveTask ??= _webSocket.ReceiveAsync(new ArraySegment<byte>(incoming), cancellationToken);

                var task = await Task.WhenAny(_receiveTask, _stateHasChangedTcs.Task);

                if (task == _receiveTask)
                {
                    var result = _receiveTask.Result;

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    await HandleAsync(result, incoming);
                    _receiveTask = null;
                }
                else
                {
                    _stateHasChangedTcs = new TaskCompletionSource();

                    await UpdateControlAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch
        {
            status = WebSocketCloseStatus.InternalServerError;
            message = "Internal Server Error";
        }

        try
        {
            await _webSocket.CloseAsync(status, message, default);
        }
        catch
        {
            // ignored
        }

        await Disconnected.InvokeAsync(this, EventArgs.Empty);
    }

    private async ValueTask HandleAsync(WebSocketReceiveResult result, byte[] incoming)
    {
        var memory = incoming.AsMemory(0, result.Count);
        var command = JsonSerializer.Deserialize(memory.Span, JsonContext.Default.WebSocketCommand);

        if (command is null)
        {
            return;
        }

        Page.IsPostBack = true;

        var pageManager = Context.RequestServices.GetRequiredService<IPageManager>();
        await InvokePostbackAsync(default, null);
        await pageManager.TriggerPostBackAsync(Page, command.EventTarget, command.EventArgument);
        Page.ClearChangedPostDataConsumers();

        if (Form != null)
        {
            await Form.OnSubmitAsync(default);
        }

        await UpdateControlAsync();

        Page.IsPostBack = false;

        var scopedControlContainer = Context.RequestServices.GetService<ScopedControlContainer>();

        if (scopedControlContainer != null)
        {
            await scopedControlContainer.DisposeFloatingControlsAsync();
        }
    }

    private async Task UpdateControlAsync(CancellationToken token = default)
    {
        _options ??= Context.RequestServices.GetService<IOptions<WebFormsCoreOptions>>() ?? Options.Create(new WebFormsCoreOptions());

        using var memory = MemoryStreamManager.GetStream();
        await using var writer = new StreamHtmlTextWriter(memory);

        writer.Write(PageManager.ToJavaScriptOptions(_options.Value));
        writer.Write('|');

        Context.Response.Body = new FlushHtmlStream(memory, writer);

        await InvokePreRenderAsync(default, null);
        await RenderAsync(writer, token);
        await writer.FlushAsync();

        var length = (int) memory.Length;
        var buffer = memory.GetBuffer();

        await _webSocket.SendAsync(buffer.AsMemory(0, length), WebSocketMessageType.Text, true, token);

        Context.Response.Body = Stream.Null;
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        if (!Visible)
        {
            return;
        }

        writer.AddAttribute("id", UniqueID);

        // Ensure we're not nesting stream panels
        if (Page.ActiveStreamPanel is null || Page.ActiveStreamPanel == this)
        {
            writer.AddAttribute("data-wfc-stream", null);
        }

        await writer.RenderBeginTagAsync("div");

        if (ProcessControl)
        {
            await base.RenderAsync(writer, token);
        }

        await writer.RenderEndTagAsync();
    }
}
