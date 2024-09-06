using WebFormsCore.TestFramework;
using WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls;

public class HtmlGenericControlTest
{
    [Theory, ClassData(typeof(TestTypeData))]
    public async Task PageWithControlAndAttributes(TestType type)
    {
        await using var result = await StartAsync<DivAttributes>(type);

        var content = result.Control.FindControl("content");
        var element = result.GetRequiredElement(content);

        Assert.Equal("color: red;", await element.GetAttributeAsync("style"));
        Assert.Equal("bar", await element.GetAttributeAsync("data-foo"));
    }
}
