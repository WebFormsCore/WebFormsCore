using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IO;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore.UI.WebControls;

[JsonSerializable(typeof(WebSocketCommand))]
internal partial class JsonContext : JsonSerializerContext
{
}

internal record WebSocketCommand(
    [property: JsonPropertyName("t"), Required] string EventTarget,
    [property: JsonPropertyName("a")] string? EventArgument
);

public class StreamPanel : Control, INamingContainer
{
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();

    private WebSocket _webSocket = null!;
    private Task<WebSocketReceiveResult>? _receiveTask;
    private bool _prerender;
#if NET
    private TaskCompletionSource _stateHasChangedTcs = new();
#else
    private TaskCompletionSource<bool> _stateHasChangedTcs = new();
#endif

    public event AsyncEventHandler<StreamPanel, EventArgs>? Connected;

    public event AsyncEventHandler<StreamPanel, EventArgs>? Disconnected;

    public bool IsConnected { get; internal set; }

    public bool Prerender
    {
        get => _prerender;
        set
        {
            if (_state >= ControlState.FrameworkInitialized)
            {
                throw new InvalidOperationException("Cannot change prerender after framework initialization.");
            }

            _prerender = value;
        }
    }

    protected override bool ProcessControl => IsConnected || _prerender;

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

#if NET
        _stateHasChangedTcs.TrySetResult();
#else
        _stateHasChangedTcs.TrySetResult(true);
#endif
    }

    internal async Task StartAsync(IHttpContext context, WebSocket websocket)
    {
        context.Response.Body = Stream.Null;

        _webSocket = websocket;

        await Connected.InvokeAsync(this, EventArgs.Empty);
        await UpdateControlAsync();

        var incoming = new byte[1024];

        var status = WebSocketCloseStatus.NormalClosure;
        var message = "Done";

        try
        {
            while (true)
            {
                _receiveTask ??= _webSocket.ReceiveAsync(new ArraySegment<byte>(incoming), default);

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
                    await UpdateControlAsync();

#if NET
                    _stateHasChangedTcs = new TaskCompletionSource();
#else
                    _stateHasChangedTcs = new TaskCompletionSource<bool>();
#endif
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

        await InvokePostbackAsync(default, null, command.EventTarget, command.EventArgument);
        await Page.RaiseChangedEventsAsync(default);
        Page.ClearChangedPostDataConsumers();

        if (Form != null)
        {
            await Form.OnSubmitAsync(default);
        }

        await InvokePreRenderAsync(default, null);

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
        using var memory = MemoryStreamManager.GetStream();
        await using var writer = new StreamHtmlTextWriter(memory);

        Context.Response.Body = new FlushHtmlStream(memory, writer);

        await RenderAsync(writer, token);
        await writer.FlushAsync();

        var length = (int) memory.Length;
        var buffer = memory.GetBuffer();

#if NET
        await _webSocket.SendAsync(buffer.AsMemory(0, length), WebSocketMessageType.Text, true, token);
#else
        await _webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, length), WebSocketMessageType.Text, true, token);
#endif

        Context.Response.Body = Stream.Null;
    }

    public override async ValueTask RenderAsync(HtmlTextWriter writer, CancellationToken token)
    {
        writer.AddAttribute("id", UniqueID);
        writer.AddAttribute("data-wfc-stream", null);
        await writer.RenderBeginTagAsync("div");

        if (ProcessControl)
        {
            await base.RenderAsync(writer, token);
        }

        await writer.RenderEndTagAsync();
    }
}
