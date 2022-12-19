using Microsoft.AspNetCore.Hosting;

namespace WebFormsCore;

public class WebFormsEnvironment : IWebFormsEnvironment
{
    private readonly IWebHostEnvironment _environment;

    public WebFormsEnvironment(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string ContentRootPath => _environment.ContentRootPath;

    public bool EnableControlWatcher => true; // TODO: Make this configurable
}
