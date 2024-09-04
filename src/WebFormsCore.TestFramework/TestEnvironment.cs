using System;

namespace WebFormsCore;

internal class TestEnvironment : IWebFormsEnvironment
{
    public string ContentRootPath => AppContext.BaseDirectory;

    public bool EnableControlWatcher => false;

    public bool CompileInBackground => false;
}