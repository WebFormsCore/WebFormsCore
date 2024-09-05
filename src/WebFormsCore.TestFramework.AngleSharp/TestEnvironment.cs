namespace WebFormsCore.TestFramework.AngleSharp;

internal class TestEnvironment : IWebFormsEnvironment
{
    public string? ContentRootPath => AppContext.BaseDirectory;

    public bool EnableControlWatcher => false;

    public bool CompileInBackground => false;
}