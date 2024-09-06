using WebFormsCore.Tests.Controls.HtmlGenericControls.Pages;

namespace WebFormsCore.Tests.Controls.HtmlGenericControls;

public class HtmlGenericControlTest
{
    [Fact]
    public async Task PageWithControlAndAttributes()
    {
        await using var result = await StartAsync<DivAttributes>();

        var content = result.Control.FindControl("content");
        var element = result.GetRequiredElement(content);

        Assert.Equal("color:red;", await element.GetAttributeAsync("style"));
        Assert.Equal("bar", await element.GetAttributeAsync("data-foo"));
    }
}
