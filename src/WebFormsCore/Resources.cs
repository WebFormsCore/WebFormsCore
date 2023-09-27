using System.IO;
using WebFormsCore.UI.HtmlControls;

namespace WebFormsCore;

internal static class Resources
{
    public static readonly string Script;

    static Resources()
    {
        using var resource = typeof(Resources).Assembly.GetManifestResourceStream("WebFormsCore.Scripts.form.min.js");
        using var reader = new StreamReader(resource!);
        Script = reader.ReadToEnd();
    }
}
