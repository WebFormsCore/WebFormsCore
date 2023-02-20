using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebFormsCore.Internal;

internal class WebFormsApplications : IWebFormsApplication
{
    private readonly IControlManager _controlManager;
    private readonly IWebFormsEnvironment _environment;

    public WebFormsApplications(IWebFormsEnvironment environment, IControlManager controlManager)
    {
        _environment = environment;
        _controlManager = controlManager;
    }

    public string? GetPath(HttpContext context)
    {
        if (string.IsNullOrEmpty(context.Request.Path)) return null;

        var fullPath = Path.Combine(_environment.ContentRootPath, context.Request.Path.ToString().TrimStart('/'));

        if (!_controlManager.TryGetPath(fullPath, out var path) || !File.Exists(fullPath))
        {
            return null;
        }

        return path;
    }

    public Task<bool> ProcessAsync(HttpContext context, string path, IServiceProvider provider, CancellationToken token)
    {
        return _controlManager.RenderPageAsync(
            context,
            provider,
            path,
            context.Response.GetOutputStream(),
            token
        );
    }
}
