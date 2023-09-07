using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public class PageManager : IPageManager
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly IControlManager _controlManager;
    private readonly IOptions<WebFormsCoreOptions> _options;

    public PageManager(IControlManager controlManager, IOptions<WebFormsCoreOptions> options)
    {
        _controlManager = controlManager;
        _options = options;
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
        page.Initialize(context);

        await page.InitAsync(token);

        if (context.Request.Query.TryGetValue("__panel", out var panel) && context.WebSockets.IsWebSocketRequest)
        {
            await RenderStreamPanelAsync(page, panel, context, token);
            return;
        }

        var control = await page.ProcessRequestAsync(token);
        var response = context.Response;

        if (response.HasStarted)
        {
            await response.Body.FlushAsync(token);
            return;
        }

        var stream = context.Response.Body;
        using var writer = new StreamHtmlTextWriter(stream);

        context.Response.ContentType = "text/html; charset=utf-8";

        if (context.Request.Query.ContainsKey("__external"))
        {
            await RenderExternalPageAsync(context, writer, page, token);
        }
        else
        {
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

            await control.RenderAsync(writer, token);
        }

        await writer.FlushAsync();
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
        await page.ProcessRequestAsync(token);

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
