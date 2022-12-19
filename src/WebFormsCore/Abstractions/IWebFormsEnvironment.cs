namespace WebFormsCore;

public interface IWebFormsEnvironment
{
    string ContentRootPath { get; }

    bool EnableControlWatcher { get; }
}
