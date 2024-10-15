using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using WebFormsCore.Features;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class ControlExtensions
{
    public static IElement FindBrowserElement(this Control control)
    {
        return control.Context.Features
            .GetRequiredFeature<ITestContextFeature>().TestContext
            .GetRequiredElement(control);
    }

    public static ValueTask ClickAsync(this Control control)
    {
        return control.FindBrowserElement().ClickAsync();
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
