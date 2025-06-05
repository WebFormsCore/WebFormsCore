using System.Threading.Tasks;
using WebFormsCore.Features;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class ControlExtensions
{
    public static IElement FindBrowserElement(this Control control)
    {
        return control.Context.Features
            .Get<ITestContextFeature>()!.TestContext
            .GetRequiredElement(control);
    }

    public static ValueTask PostBackAsync(this Control control, string? argument = null, PostBackOptions? options = null)
    {
        return control.FindBrowserElement().PostBackAsync(argument, options);
    }

    public static ValueTask ClickAsync(this Control control)
    {
        return control.FindBrowserElement().ClickAsync();
    }

    public static ValueTask SelectAsync(this Control control, string value)
    {
        return control.FindBrowserElement().SelectAsync(value);
    }

    public static ValueTask ClearAsync(this Control control)
    {
        return control.FindBrowserElement().ClearAsync();
    }

    public static ValueTask TypeAsync(this Control control, string text)
    {
        return control.FindBrowserElement().TypeAsync(text);
    }

    public static ValueTask<string> GetBrowserAttributeAsync(this Control control, string name)
    {
        return control.FindBrowserElement().GetAttributeAsync(name);
    }

    public static string GetBrowserText(this Control control)
    {
        return control.FindBrowserElement().Text;
    }
}
