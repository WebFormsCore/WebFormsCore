using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore;

public interface IControlManager
{
    Type GetType(string path);

    ValueTask<Type> GetTypeAsync(string path);

    bool TryGetPath(string fullPath, [NotNullWhen(true)] out string? path);
}

public interface IPageManager
{
    Task<Page> RenderPageAsync(
        IHttpContext context,
        IServiceProvider provider,
        string path,
        Stream stream,
        CancellationToken token);

    Task<Page> RenderPageAsync(
        IHttpContext context,
        IServiceProvider provider,
        Type pageType,
        Stream stream,
        CancellationToken token);

    Task RenderPageAsync(IHttpContext context,
        IServiceProvider provider,
        Page page,
        Stream stream,
        CancellationToken token);
}

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
        IServiceProvider provider,
        string path,
        Stream stream,
        CancellationToken token)
    {
        var pageType = await _controlManager.GetTypeAsync(path);

        return await RenderPageAsync(context, provider, pageType, stream, token);
    }

    public async Task<Page> RenderPageAsync(
        IHttpContext context,
        IServiceProvider provider,
        Type pageType,
        Stream stream,
        CancellationToken token)
    {
        var page = (Page) ActivatorUtilities.GetServiceOrCreateInstance(provider, pageType);
        await RenderPageAsync(context, provider, page, stream, token);
        return page;
    }

    public async Task RenderPageAsync(
        IHttpContext context,
        IServiceProvider provider,
        Page page,
        Stream stream,
        CancellationToken token)
    {
        page.Initialize(provider, context);

        await page.ProcessRequestAsync(token);

        var response = context.Response;

        if (page.Csp.Enabled)
        {
            response.Headers["Content-Security-Policy"] = page.Csp.ToString();
        }

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
