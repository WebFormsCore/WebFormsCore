﻿using System;
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
    private readonly PageFactory _pageFactory;
    private readonly IWebFormsEnvironment _environment;

    public WebFormsApplications(IWebFormsEnvironment environment, PageFactory pageFactory)
    {
        _environment = environment;
        _pageFactory = pageFactory;
    }

    public async Task<bool> ProcessAsync(HttpContext context, IServiceProvider provider, CancellationToken token)
    {
        var path = Path.Combine(_environment.ContentRootPath, "Default.aspx");
        var pageType = await _pageFactory.GetTypeAsync(path);
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
