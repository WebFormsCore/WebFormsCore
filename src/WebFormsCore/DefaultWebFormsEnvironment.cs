using System;
using System.IO;

namespace WebFormsCore;

public class DefaultWebFormsEnvironment : IWebFormsEnvironment
{
    public string? ContentRootPath => Directory.GetCurrentDirectory();

    public bool EnableControlWatcher => true; // TODO: Make this configurable

    public bool CompileInBackground => true; // TODO: Make this configurable
}

internal class MinimalWebFormsEnvironment : IWebFormsEnvironment
{
    public string? ContentRootPath => null;

    public bool EnableControlWatcher => false;

    public bool CompileInBackground => false;
}