using WebFormsCore.Tests.Pages;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public partial class PageTest
{
    [Fact]
    public async Task PageWithControl()
    {
        DisposableControl[] controls;

        await using (var result = await RenderAsync("Pages/Page.aspx", enableViewState: false))
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
        await using var result = await RenderAsync("Pages/DivAttributes.aspx", enableViewState: false);

        await Verify(result.Html);
    }

    [Fact]
    public async Task PageWithClickEvent()
    {
        await using var result = await RenderAsync<ClickTest>();

        Assert.Empty(result.Page.lblResult.Text);

        await result.PostbackAsync(result.Page.btnSetResult);

        Assert.Equal("Success", result.GetElement(result.Page.lblResult).TextContent);
    }

    [Fact]
    public async Task PageLargeViewState()
    {
        await using var result = await RenderAsync<LargeViewStateTest>();

        Assert.Equal(100, result.Document.QuerySelectorAll("[data-id]").Length);
        await result.PostbackAsync("[data-id='10'] .btn");

        Assert.Equal("10", result.Page.SelectedId);
    }
}
