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

    public async Task<bool> ProcessAsync(HttpContext context, IServiceProvider provider, CancellationToken token)
    {
        var pageType = await _viewManager.GetTypeAsync("Default.aspx");
        var page = (Page)Activator.CreateInstance(pageType)!;

        page.Initialize(provider, context);

        var control = await page.ProcessRequestAsync(token);

        var response = context.Response;

        if (page.Csp.Enabled)
        {
            response.Headers["Content-Security-Policy"] = page.Csp.ToString();
        }

        var stream = response.OutputStream;

#if NETFRAMEWORK
        using var textWriter = new StreamWriter(stream) { NewLine = "\n" };
        using var writer = new HtmlTextWriter(textWriter, stream);
#else
        await using var textWriter = new StreamWriter(stream) { NewLine = "\n" };
        await using var writer = new HtmlTextWriter(textWriter, stream);
#endif

        context.Response.ContentType = "text/html";
        await control.RenderAsync(writer, token);
        await writer.FlushAsync();

        return true;
    }
}
