using WebFormsCore.Tests.Pages;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

[UsesVerify]
public partial class PageTest
{
    [Fact]
    public async Task PageWithControl()
    {
        DisposableControl[] controls;

        await using (var result = await RenderAsync("Pages/Page.aspx"))
        {
            controls = result.Page.EnumerateControls()
                .OfType<DisposableControl>()
                .ToArray();

            Assert.Equal(2, controls.Length);

            Assert.False(controls[0].IsDisposed, "Control in Page should not be disposed");
            Assert.False(controls[1].IsDisposed, "Dynamic control should not be disposed");

            await Verify(result.Html);
        }

        Assert.True(controls[0].IsDisposed, "Control in Page should be disposed");
        Assert.True(controls[1].IsDisposed, "Dynamic control should be disposed");
    }

    [Fact]
    public async Task PageWithControlAndAttributes()
    {
        await using var result = await RenderAsync("Pages/DivAttributes.aspx");

        await Verify(result.Html);
    }

    [Fact]
    public async Task TypedRepeater()
    {
        await using var result = await RenderAsync("Pages/TypedRepeater.aspx");

        await Verify(result.Html);
    }
}
