using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpStack;
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
        if (string.IsNullOrEmpty(path) || !_controlManager.TryGetPath(path, out var result))
        {
            return null;
        }

        return result;
    }

    public Task<Page> ProcessAsync(IHttpContext context, string path, CancellationToken token)
    {
        return _pageManager.RenderPageAsync(context, path, token);
    }
}
