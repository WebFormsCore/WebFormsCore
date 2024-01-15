using System.IO;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

internal static class Resources
{
    public static readonly string Script;
    public static readonly string Polyfill;

    static Resources()
    {
        Script = GetString("form.min.js");
        Polyfill = GetString("webforms-polyfill.min.js");
    }

    private static string GetString(string fileName)
    {
        using var resource = typeof(Resources).Assembly.GetManifestResourceStream($"WebFormsCore.Scripts.{fileName}");
        using var reader = new StreamReader(resource!);
        return reader.ReadToEnd().Trim();
    }
}
