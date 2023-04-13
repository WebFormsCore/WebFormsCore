using System;

namespace WebFormsCore;

public class DefaultWebFormsEnvironment : IWebFormsEnvironment
{
    public string? ContentRootPath => AppContext.BaseDirectory;

    public bool EnableControlWatcher => true; // TODO: Make this configurable
}
