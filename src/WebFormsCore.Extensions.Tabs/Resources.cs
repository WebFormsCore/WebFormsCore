using System.IO;

namespace WebFormsCore;

internal static class Resources
{
    public static readonly string Script;

    static Resources()
    {
        using var resource = typeof(Resources).Assembly.GetManifestResourceStream("WebFormsCore.Scripts.tabs.min.js");
        using var reader = new StreamReader(resource!);
        Script = reader.ReadToEnd();
    }
}
