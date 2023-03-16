using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebFormsCore.UI;

namespace WebFormsCore.Internal;

internal class WebFormsApplications : IWebFormsApplication
{
    private readonly IPageManager _pageManager;
    private readonly IControlManager _controlManager;
    private readonly IWebFormsEnvironment _environment;

    public WebFormsApplications(IWebFormsEnvironment environment, IPageManager pageManager, IControlManager controlManager)
    {
        _environment = environment;
        _pageManager = pageManager;
        _controlManager = controlManager;
    }

    public string? GetPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (_environment.ContentRootPath is null)
        {
            return null;
        }

        var fullPath = Path.Combine(_environment.ContentRootPath, path.TrimStart('/'));

        if (!_controlManager.TryGetPath(fullPath, out var result) || !File.Exists(fullPath))
        {
            return null;
        }

        return result;
    }

    public Task<Page> ProcessAsync(IHttpContext context, string path, IServiceProvider provider, CancellationToken token)
    {
        return _pageManager.RenderPageAsync(
            context,
            provider,
            path,
            context.Response.Body,
            token
        );
    }
}
