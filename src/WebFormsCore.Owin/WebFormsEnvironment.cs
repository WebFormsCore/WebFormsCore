using System;
using System.Web;

namespace WebFormsCore;

public class WebFormsEnvironment : IWebFormsEnvironment
{
    public string ContentRootPath => AppContext.BaseDirectory;

    public bool EnableControlWatcher => true; // TODO: Make this configurable
}
