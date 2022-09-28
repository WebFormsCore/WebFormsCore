using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CodeAnalysis.Emit;
using WebFormsCore.Compiler;
using WebFormsCore.UI;

namespace WebFormsCore.Internal;

internal class WebFormsApplications : IWebFormsApplication
{
    private readonly ViewManager _viewManager;
    private readonly IWebFormsEnvironment _environment;

    public WebFormsApplications(IWebFormsEnvironment environment, ViewManager viewManager)
    {
        _environment = environment;
        _viewManager = viewManager;
    }

    public string? GetPath(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.Request.Path)) return null;

        var fullPath = Path.Combine(_environment.ContentRootPath, context.Request.Path.TrimStart('/'));

        if (!_viewManager.TryGetPath(fullPath, out var path) || !File.Exists(fullPath))
        {
            return null;
        }

        return path;
    }

    public async Task<bool> ProcessAsync(HttpContext context, string path, IServiceProvider provider, CancellationToken token)
    {
        var pageType = await _viewManager.GetTypeAsync(path);
        var page = (Page)Activator.CreateInstance(pageType)!;

        page.Initialize(provider, context);

        await page.ProcessRequestAsync(token);

        var response = context.Response;

        if (page.Csp.Enabled)
        {
            response.Headers["Content-Security-Policy"] = page.Csp.ToString();
        }

        var stream = response.OutputStream;

#if NET
        // await using
        await
#endif
        using var textWriter = new StreamWriter(stream)
        {
            NewLine = "\n",
            AutoFlush = false
        };

        await using var writer = new HtmlTextWriter(textWriter, stream);

        context.Response.ContentType = "text/html";
        await page.RenderAsync(writer, token);
        await writer.FlushAsync();

        return true;
    }
}
