using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WebFormsCore.Events;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public class PageManager : IPageManager
{
    private readonly IViewStateManager _viewStateManager;
    private readonly IControlManager _controlManager;
    private readonly IOptions<WebFormsCoreOptions> _options;

    public PageManager(
        IControlManager controlManager,
        IViewStateManager viewStateManager,
        IOptions<WebFormsCoreOptions>? options = null)
    {
        _controlManager = controlManager;
        _options = options ?? Options.Create(new WebFormsCoreOptions());
        _viewStateManager = viewStateManager;
    }

    public async Task<Page> RenderPageAsync(
        IHttpContext context,
        string path,
        CancellationToken token)
    {
        var pageType = await _controlManager.GetTypeAsync(path);

        return await RenderPageAsync(context, pageType, token);
    }

    public async Task<Page> RenderPageAsync(
        IHttpContext context,
        Type pageType,
        CancellationToken token)
    {
        var objectActivator = context.RequestServices.GetRequiredService<IWebObjectActivator>();
        var page = (Page)objectActivator.CreateControl(pageType);
        await RenderPageAsync(context, page, token);
        return page;
    }

    public async Task RenderPageAsync(
        IHttpContext context,
        Page page,
        CancellationToken token)
    {
        page.SetContext(context);

        await InitPageAsync(page, token);

        if (context.Request.Query.TryGetValue("__panel", out var panel) && context.WebSockets.IsWebSocketRequest)
        {
            await RenderStreamPanelAsync(page, panel, context, token);
            return;
        }

        var control = await ProcessRequestAsync(context, page, token);

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

        foreach (var pageService in pageServices)
        {
            await pageService.BeforeInitializeAsync(page, token);
        }

        await internalPage.InvokeInitAsync(token);

        foreach (var pageService in pageServices)
        {
            await pageService.AfterInitializeAsync(page, token);
        }
    }

    /// <summary>
    /// Load the view state of the page and its active form.
    /// </summary>
    /// <param name="context">Context of the request.</param>
    /// <param name="page">Page to load the view state for.</param>
    /// <returns>The active form if it was loaded.</returns>
    private async ValueTask<HtmlForm?> LoadViewStateAsync(IHttpContext context, Page page)
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

        if (request.Form.TryGetValue("wfcForm", out var formId) &&
            request.Form.TryGetValue("wfcFormState", out formState))
        {
            form = page.Forms.FirstOrDefault(i => i.UniqueID == formId);
        }

        if (form is null or { UpdatePage: true} &&
            request.Form.TryGetValue("wfcPageState", out var pageState))
        {
            await _viewStateManager.LoadAsync(page, pageState.ToString()!);
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
    /// <param name="token">Cancellation token.</param>
    /// <returns>The control that is loaded and should be rendered.</returns>
    private async Task<Control?> ProcessRequestAsync(IHttpContext context, Page page, CancellationToken token)
    {
        var form = await LoadViewStateAsync(context, page);
        var serviceProvider = context.RequestServices;
        var target = form is null or { UpdatePage: true } ? (Control)page : form;
        var pageServices = serviceProvider.GetServices<IPageService>() as IPageService[] ?? Array.Empty<IPageService>();

        page.ActiveForm = form;

        foreach (var pageService in pageServices)
        {
            await pageService.BeforeLoadAsync(page, token);
        }

        await target.InvokeLoadAsync(token, form);

        foreach (var service in pageServices)
        {
            await service.AfterLoadAsync(page, token);
        }

        if (page.IsPostBack)
        {
            foreach (var service in pageServices)
            {
                await service.BeforePostbackAsync(page, token);
            }

            if (context.Request.Form.TryGetValue("wfcTarget", out var eventTarget))
            {
                var eventArgument = context.Request.Form.TryGetValue("wfcArgument", out var eventArgumentValue)
                    ? eventArgumentValue.ToString()
                    : string.Empty;

                await target.InvokePostbackAsync(token, form);

                var postbackControl = page.FindControl(eventTarget.ToString());

                if (postbackControl is IPostBackAsyncEventHandler asyncEventHandler)
                {
                    await asyncEventHandler.RaisePostBackEventAsync(eventArgument);
                }
                else if (postbackControl is IPostBackEventHandler eventHandler)
                {
                    eventHandler.RaisePostBackEvent(eventArgument);
                }
            }
            else
            {
                await page.ValidateAsync();
            }

            await page.RaiseChangedEventsAsync(token);

            if (form != null)
            {
                await form.OnSubmitAsync(token);
            }

            foreach (var service in pageServices)
            {
                await service.AfterPostbackAsync(page, token);
            }
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

    /// <summary>
    /// Render the page to the response.
    /// </summary>
    /// <param name="page">Page instance.</param>
    /// <param name="context">Context of the request.</param>
    /// <param name="controlToRender">Control to render.</param>
    /// <param name="writer">Writer to render to.</param>
    /// <param name="token">Cancellation token.</param>
    private async Task RenderPageAsync(Page page, IHttpContext context, Control controlToRender, HtmlTextWriter writer, CancellationToken token)
    {
        var response = context.Response;

        if (_options.Value.EnableSecurityHeaders)
        {
            response.Headers["X-Frame-Options"] = "DENY";
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Referrer-Policy"] = "no-referrer";

            if (page.Csp.Enabled)
            {
                response.Headers["Content-Security-Policy"] = page.Csp.ToString();
            }
        }

        await controlToRender.RenderAsync(writer, token);
    }

    private async Task RenderStreamPanelAsync(Page page, string panel, IHttpContext context, CancellationToken token)
    {
        if (!_options.Value.AllowExternal)
        {
            throw new InvalidOperationException("Stream panels are not allowed.");
        }

        var streamControl = page.FindControl(panel);

        if (streamControl is not StreamPanel streamPanel)
        {
            throw new InvalidOperationException($"Panel '{panel}' is not a StreamPanel.");
        }

        page.ActiveStreamPanel = streamPanel;
        streamPanel.IsConnected = true;

        streamPanel.InvokeFrameworkInit(token);
        await streamPanel.InvokeInitAsync(token);
        await ProcessRequestAsync(context, page, token);

        context.WebSockets.AcceptWebSocketRequest(streamPanel.StartAsync);
    }

    private async Task RenderExternalPageAsync(IHttpContext context, HtmlTextWriter writer, Page page, CancellationToken token)
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

            await page.ClientScript.RenderStartupHead(writer);
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

    private static string ToAbsolute(string? value, IHttpRequest request)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.ToString();
        }

        var builder = new UriBuilder(request.Scheme, request.Host)
        {
            Path = value
        };

        return builder.Uri.ToString();
    }
}
