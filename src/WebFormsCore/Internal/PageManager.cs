using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WebFormsCore.Events;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public partial class PageManager : IPageManager
{
    private readonly IViewStateManager _viewStateManager;
    private readonly IControlManager _controlManager;
    private readonly IOptions<WebFormsCoreOptions> _options;
    private readonly IOptions<ViewStateOptions> _viewStateOptions;

    public PageManager(
        IControlManager controlManager,
        IViewStateManager viewStateManager,
        IOptions<WebFormsCoreOptions>? options = null,
        IOptions<ViewStateOptions>? viewStateOptions = null)
    {
        _controlManager = controlManager;
        _options = options ?? Options.Create(new WebFormsCoreOptions());
        _viewStateOptions = viewStateOptions ?? Options.Create(new ViewStateOptions());
        _viewStateManager = viewStateManager;
    }

    public async Task<Page> RenderPageAsync(
        HttpContext context,
        string path,
        CancellationToken token)
    {
        var pageType = await _controlManager.GetTypeAsync(path);

#pragma warning disable IL2062 // This will be set by the source generator
        return await RenderPageAsync(context, pageType, token);
#pragma warning restore IL2062
    }

    public async Task<Page> RenderPageAsync(
        HttpContext context,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type pageType,
        CancellationToken token)
    {
        var objectActivator = context.RequestServices.GetRequiredService<IWebObjectActivator>();
        var page = (Page)objectActivator.CreateControl(pageType);
        await RenderPageAsync(context, page, token);
        return page;
    }

    public async Task RenderPageAsync(
        HttpContext context,
        Page page,
        CancellationToken token)
    {
        if (context.Request.Query.TryGetValue("__panel", out var panel) &&
            panel.ToString() is { Length: > 0 } panelStr &&
            context.WebSockets.IsWebSocketRequest)
        {
            await RenderStreamPanelAsync(page, panelStr, context, token);
            return;
        }

        page.SetContext(context);
        await InitPageAsync(page, token);

        var control = await ProcessRequestAsync(context, page, render: true, token);

        if (control is null)
        {
            return;
        }

        var response = context.Response;

        if (response.HasStarted)
        {
            await response.Body.FlushAsync(token);
            return;
        }

        if (page.IsPostBack)
        {
            var options = _options.Value;

            response.Headers["x-wfc-options"] = ToJavaScriptOptions(options);
        }

        var stream = context.Response.Body;
        await using var writer = new StreamHtmlTextWriter(stream);

        context.Response.Body = new FlushHtmlStream(stream, writer);
        context.Response.ContentType = "text/html; charset=utf-8";

        if (context.Request.Query.ContainsKey("__external"))
        {
            await RenderExternalPageAsync(context, writer, page, token);
        }
        else
        {
            await RenderPageAsync(page, context, control, writer, token);
        }

        await writer.FlushAsync();

        context.Response.Body = stream;
    }

    internal static string ToJavaScriptOptions(WebFormsCoreOptions options)
    {
        return options switch
        {
            { RenderScriptOnPostBack: true, RenderStylesOnPostBack: true } => "11",
            { RenderScriptOnPostBack: true } => "10",
            { RenderStylesOnPostBack: true } => "01",
            _ => "00"
        };
    }

    /// <summary>
    /// Invoke <see cref="Control.FrameworkInitialize"/> and <see cref="Control.OnInit"/> on the page and all its controls.
    /// </summary>
    /// <param name="page">Page to initialize.</param>
    /// <param name="token">Cancellation token.</param>
    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private static async Task InitPageAsync(Page page, CancellationToken token)
    {
        var internalPage = (IInternalPage) page;
        var serviceProvider = internalPage.Context.RequestServices;
        var pageServices = serviceProvider.GetServices<IPageService>() as IPageService[] ?? Array.Empty<IPageService>();

        internalPage.InvokeFrameworkInit(token);

        await internalPage.InvokePreInitAsync(token);

        foreach (var pageService in pageServices)
        {
            await pageService.BeforeInitializeAsync(page, token);
        }

        await internalPage.InvokeInitAsync(token);

        foreach (var pageService in pageServices)
        {
            await pageService.AfterInitializeAsync(page, token);
        }

        if (page.EarlyHints is { Enabled: true, IsEmpty: false })
        {
            await SendEarlyHints(page);
        }
    }

    const string BrowserPattern = @"(?'BrowserName'Chrome|Firefox)/(?'Version'\d+)";

    [GeneratedRegex(BrowserPattern, RegexOptions.Compiled)]
    private static partial Regex UserAgentRegex();

#if !WASM
    private static async ValueTask SendEarlyHints(Page page)
    {
        var userAgent = page.Context.Request.Headers["User-Agent"].ToString();

        if (string.IsNullOrEmpty(userAgent))
        {
            return;
        }

        var match = UserAgentRegex().Match(userAgent);

        if (!match.Success)
        {
            return;
        }

        var versionValue = match.Groups["Version"].ValueSpan;
        var browserNameValue = match.Groups["BrowserName"].ValueSpan;

        if (!int.TryParse(versionValue, out var version))
        {
            return;
        }

        if (browserNameValue.Equals("Chrome", StringComparison.OrdinalIgnoreCase))
        {
            if (version < 103)
            {
                return;
            }
        }
        else if (browserNameValue.Equals("Firefox", StringComparison.OrdinalIgnoreCase))
        {
            if (version < 120)
            {
                return;
            }
        }
        else
        {
            // TODO: Safari doesn't support 'preload' yet
            return;
        }


        var accept = page.Context.Request.Headers.Accept;
        var isHtml = false;

        foreach (var i in accept)
        {
            if (i != null && i.Contains("text/html", StringComparison.OrdinalIgnoreCase))
            {
                isHtml = true;
                break;
            }
        }

        if (isHtml)
        {
            var earlyHints = page.EarlyHints.GetLinkHeader();
            await page.Context.Send103EarlyHints(earlyHints);
            page.Context.Items["EarlyHints"] = earlyHints;
        }
    }
#else
    private static ValueTask SendEarlyHints(Page page) => default;
#endif

    /// <summary>
    /// Load the view state of the page and its active form.
    /// </summary>
    /// <param name="context">Context of the request.</param>
    /// <param name="page">Page to load the view state for.</param>
    /// <returns>The active form if it was loaded.</returns>
    private async ValueTask<HtmlForm?> LoadViewStateAsync(HttpContext context, Page page)
    {
        if (!_viewStateManager.EnableViewState)
        {
            return null;
        }

        if (!page.IsPostBack)
        {
            return null;
        }

        var request = context.Request;

        HtmlForm? form = null;
        StringValues formState = default;

        if (request.Form.TryGetValue("wfcPageState", out var pageState))
        {
            await _viewStateManager.LoadAsync(page, pageState.ToString()!);
        }

        if (request.Form.TryGetValue("wfcForm", out var formId) &&
            request.Form.TryGetValue("wfcFormState", out formState))
        {
            form = page.Forms.FirstOrDefault(i => i.UniqueID == formId);
        }

        if (form != null && !string.IsNullOrEmpty(formState))
        {
            await _viewStateManager.LoadAsync(form, formState.ToString()!);
        }

        return form;
    }

    /// <summary>
    /// Process the request and invoke the page lifecycle events.
    /// </summary>
    /// <param name="context">Context of the request.</param>
    /// <param name="page">Page to process the request for.</param>
    /// <param name="render">Whether the page should be rendered.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The control that is loaded and should be rendered.</returns>
    private async Task<Control?> ProcessRequestAsync(HttpContext context, Page page, bool render, CancellationToken token)
    {
        var form = await LoadViewStateAsync(context, page);

        return await ProcessRequestAsync(context, page, form, render, token);
    }

    private async Task<Control?> ProcessRequestAsync(HttpContext context, Page page, HtmlForm? form, bool render, CancellationToken token)
    {
        var serviceProvider = context.RequestServices;
        var target = page; // form is null or { UpdatePage: true } ? (Control)page : form;
        var pageServices = serviceProvider.GetServices<IPageService>() as IPageService[] ?? Array.Empty<IPageService>();

        page.ActiveForm = form;

        foreach (var pageService in pageServices)
        {
            await pageService.BeforeLoadAsync(page, token);
        }

        if (page.IsPostBack)
        {
            foreach (var service in pageServices)
            {
                await service.BeforePostbackAsync(page, token);
            }

            await target.InvokePostbackAsync(token, form);
        }

        await target.InvokeLoadAsync(token, form);

        foreach (var service in pageServices)
        {
            await service.AfterLoadAsync(page, token);
        }

        if (page.IsPostBack)
        {
            var targetName = context.Request.Form.TryGetValue("wfcTarget", out var eventTarget)
                ? eventTarget.ToString()
                : string.Empty;

            var argument = context.Request.Form.TryGetValue("wfcArgument", out var eventArgument)
                ? eventArgument.ToString()
                : string.Empty;

            await TriggerPostBackAsync(page, targetName, argument, token, pageServices);

            target.InvokeRegisterBackgroundControl(token);
        }

        await page.ExecuteRegisteredAsyncTasksAsync(token);

        if (!render)
        {
            return null;
        }

        if (context.Response.StatusCode is 301 or 302)
        {
            if (context.Request.Headers.ContainsKey("X-IsPostback"))
            {
                context.Response.StatusCode = 204;
                context.Response.Headers["X-Redirect-To"] = context.Response.Headers["Location"];
                context.Response.Headers.Remove("Location");
            }

            return null;
        }

        foreach (var service in pageServices)
        {
            await service.BeforePreRenderAsync(page, token);
        }

        await target.InvokePreRenderAsync(token, form);

        foreach (var service in pageServices)
        {
            await service.AfterPreRenderAsync(page, token);
        }

        return target;
    }

    public Task TriggerPostBackAsync(Page page, string? target, string? argument, CancellationToken token)
    {
        var pageServices = page.Context.RequestServices.GetServices<IPageService>() as IPageService[] ?? [];

        return TriggerPostBackAsync(page, target, argument, token, pageServices);
    }

    private static async Task TriggerPostBackAsync(Page page, string? target, string? argument, CancellationToken token, IPageService[] pageServices)
    {
        if (!string.IsNullOrEmpty(target))
        {
            var postbackControl = page.FindControl(target!);

            if (postbackControl is IPostBackAsyncEventHandler asyncEventHandler)
            {
                await asyncEventHandler.RaisePostBackEventAsync(argument);
            }
            else if (postbackControl is IPostBackEventHandler eventHandler)
            {
                eventHandler.RaisePostBackEvent(argument);
            }
        }
        else
        {
            await page.ValidateAsync();
        }

        await page.RaiseChangedEventsAsync(token);

        if (page.ActiveForm is {} form)
        {
            await form.OnSubmitAsync(token);
        }

        foreach (var service in pageServices)
        {
            await service.AfterPostbackAsync(page, token);
        }
    }

    /// <summary>
    /// Render the page to the response.
    /// </summary>
    /// <param name="page">Page instance.</param>
    /// <param name="context">Context of the request.</param>
    /// <param name="controlToRender">Control to render.</param>
    /// <param name="writer">Writer to render to.</param>
    /// <param name="token">Cancellation token.</param>
    private async Task RenderPageAsync(Page page, HttpContext context, Control controlToRender, HtmlTextWriter writer, CancellationToken token)
    {
        var response = context.Response;

        if (_options.Value.EnableSecurityHeaders)
        {
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Referrer-Policy"] = "no-referrer";

#if !WASM
            if (page.Csp.Enabled)
            {
                response.Headers["Content-Security-Policy"] = page.Csp.ToString();
            }
#endif
        }

        await controlToRender.RenderAsync(writer, token);
    }

    private async Task RenderStreamPanelAsync(Page page, string panel, HttpContext context, CancellationToken token)
    {
        await Task.CompletedTask;

        if (!_options.Value.AllowStreamPanel)
        {
            throw new InvalidOperationException("Stream panels are not allowed.");
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();

        // Wait for the viewstate to be loaded
        using (var stream = StreamPanel.MemoryStreamManager.GetStream())
        using (var owner = MemoryPool<byte>.Shared.Rent(4096))
        {
            while (true)
            {
                if (stream.Length > _viewStateOptions.Value.MaxBytes)
                {
                    throw new InvalidOperationException("ViewState is too large.");
                }

                var socketResult = await socket.ReceiveAsync(owner.Memory, token);

                stream.Write(owner.Memory.Span.Slice(0, socketResult.Count));

                if (socketResult.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                if (socketResult.EndOfMessage)
                {
                    break;
                }
            }

            stream.Position = 0;

            var formData = stream.TryGetBuffer(out var buffer)
                ? Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count)
                : Encoding.UTF8.GetString(stream.ToArray());

            // TODO: Improve parsing of query strings
            var values = new Dictionary<string, StringValues>();
            var queryString = System.Web.HttpUtility.ParseQueryString(formData);

            foreach (var key in queryString.AllKeys)
            {
                if (key is null)
                {
                    continue;
                }

                values[key] = queryString[key];
            }

            context.Request.Method = "POST";
            context.Request.Form = new FormCollection(
                values,
                context.Request.HasFormContentType ? context.Request.Form.Files : null
            );
        }

        // Initialize the page
        page.SetContext(context);
        await InitPageAsync(page, token);
        var form = await LoadViewStateAsync(context, page);

        // Find the control and start the stream
        var streamControl = page.FindControl(panel);

        if (streamControl is not StreamPanel streamPanel)
        {
            return;
        }

        page.ActiveStreamPanel = streamPanel;
        streamPanel.IsConnected = true;

        streamPanel.InvokeFrameworkInit(token);
        await streamPanel.InvokeInitAsync(token);
        await ProcessRequestAsync(context, page, form, render: true, token);

        await streamPanel.StartAsync(context, socket);
    }

    private async Task RenderExternalPageAsync(HttpContext context, HtmlTextWriter writer, Page page, CancellationToken token)
    {
        if (!_options.Value.AllowExternal)
        {
            throw new InvalidOperationException("External pages are not allowed.");
        }

        page.IsExternal = true;

        context.Response.Headers["Access-Control-Allow-Origin"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";

        if (page.Body == null)
        {
            return;
        }

        // Add all the external resources to the head
        if (!page.IsPostBack)
        {
            if (page.Header != null)
            {
                foreach (var control in page.Header.EnumerateControls())
                {
                    switch (control)
                    {
                        case HtmlLink link when link.Attributes["href"] != null:
                        {
                            var url = ToAbsolute(link.Attributes["href"], context.Request);

                            link.Attributes.AddAttributes(writer);
                            writer.RemoveAttributes(HtmlTextWriterAttribute.Href);
                            writer.AddAttribute(HtmlTextWriterAttribute.Href, url);
                            await writer.RenderSelfClosingTagAsync(HtmlTextWriterTag.Link);
                            break;
                        }
                        case HtmlLink link:
                            await link.RenderAsync(writer, token);
                            break;
                        case HtmlScript script when script.Attributes["src"] != null:
                        {
                            var url = ToAbsolute(script.Attributes["src"], context.Request);

                            script.Attributes.AddAttributes(writer);
                            writer.RemoveAttributes(HtmlTextWriterAttribute.Href);
                            writer.AddAttribute(HtmlTextWriterAttribute.Src, url);
                            await writer.RenderBeginTagAsync(HtmlTextWriterTag.Script);
                            await writer.RenderEndTagAsync();
                            break;
                        }
                        case HtmlScript script:
                            await script.RenderAsync(writer, token);
                            break;
                    }
                }
            }

            await page.ClientScript.RenderHeadEnd(writer, ScriptType.Style);
            await page.ClientScript.RenderHeadStart(writer, ScriptType.Script);
        }

        var request = context.Request;
        var baseUrl = request.Scheme + "://" + request.Host + request.Path;

        if (request.Query.Count > 0)
        {
            baseUrl += request.QueryString.Value;
        }

        writer.AddAttribute("data-wfc-base", baseUrl);

        await writer.RenderBeginTagAsync(HtmlTextWriterTag.Div);
        await page.Body.RenderChildrenInternalAsync(writer, token);
        await writer.RenderEndTagAsync();
    }

    private static string ToAbsolute(string? value, HttpRequest request)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(request.Scheme, request.Host.Value)
        {
            Path = value
        };

        return builder.Uri.ToString();
    }
}
