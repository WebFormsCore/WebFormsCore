using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore;

public class PageManager : IPageManager
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);


    private readonly IControlManager _controlManager;

    public PageManager(IControlManager controlManager)
    {
        _controlManager = controlManager;
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
        var page = (Page) ActivatorUtilities.GetServiceOrCreateInstance(context.RequestServices, pageType);
        await RenderPageAsync(context, page, token);
        return page;
    }

    public async Task RenderPageAsync(
        IHttpContext context,
        Page page,
        CancellationToken token)
    {
        page.Initialize(context);

        await page.ProcessRequestAsync(token);

        var response = context.Response;

        if (page.Csp.Enabled)
        {
            response.Headers["Content-Security-Policy"] = page.Csp.ToString();
        }

        var stream = context.Response.Body;
#if NET
        // await using
        await
#endif
            using var textWriter = new StreamWriter(stream, Utf8WithoutBom, 1024, true)
            {
                NewLine = "\n",
                AutoFlush = false
            };

        await using var writer = new HtmlTextWriter(textWriter, stream);

        context.Response.ContentType = "text/html";

        await page.RenderAsync(writer, token);
        await writer.FlushAsync();
    }
}
