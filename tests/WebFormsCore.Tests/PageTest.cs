using WebFormsCore.Tests.Pages;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

[UsesVerify]
public partial class PageTest
{
    [Fact]
    public async Task PageWithControl()
    {
        await using var result = await RenderAsync("Pages/Page.aspx");

        var controls = result.Page.EnumerateControls()
            .OfType<DisposableControl>()
            .ToArray();

        Assert.Equal(2, controls.Length);
        Assert.True(controls[0].IsDisposed, "Control in Page should be disposed");
        Assert.True(controls[1].IsDisposed, "Dynamic control should be disposed");

        await Verify(result.Html);
    }

}
