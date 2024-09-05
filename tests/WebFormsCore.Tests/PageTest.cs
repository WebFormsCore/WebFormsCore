using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebFormsCore.Tests.Pages;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public partial class PageTest
{
    [Fact]
    public async Task PageWithControl()
    {
        DisposableControl[] controls;

        await using (var result = await RenderAsync<AssemblyControlTypeProvider>("Pages/Page.aspx", enableViewState: false))
        {
            controls = result.Control.EnumerateControls()
                .OfType<DisposableControl>()
                .ToArray();

            Assert.Equal(2, controls.Length);

            Assert.False(controls[0].IsDisposed, "Control in Page should not be disposed");
            Assert.False(controls[1].IsDisposed, "Dynamic control should not be disposed");

            await Verify(result.GetHtmlAsync());
        }

        Assert.True(controls[0].IsDisposed, "Control in Page should be disposed");
        Assert.True(controls[1].IsDisposed, "Dynamic control should be disposed");
    }

    [Fact]
    public async Task PageWithControlAndAttributes()
    {
        await using var result = await RenderAsync<AssemblyControlTypeProvider>("Pages/DivAttributes.aspx", enableViewState: false);

        await Verify(await result.GetHtmlAsync());
    }

    [Fact]
    public async Task PageWithClickEvent()
    {
        await using var result = await RenderAsync<ClickTest, AssemblyControlTypeProvider>();

        Assert.Empty(result.Control.lblResult.Text);

        await result.GetRequiredElement(result.Control.btnSetResult).ClickAsync();

        Assert.Equal("Success", result.GetRequiredElement(result.Control.lblResult).Text);
    }

    [Fact]
    public async Task PageLargeViewState()
    {
        await using var result = await RenderAsync<LargeViewStateTest, AssemblyControlTypeProvider>();

        Assert.Equal(100, result.QuerySelectorAll("[data-id]").Length);
        await result.QuerySelectorRequired("[data-id='10'] .btn").ClickAsync();

        Assert.Equal("10", result.Control.SelectedId);
    }

    [Fact]
    public async Task BrowserTest()
    {
        await using var ctx = await SeleniumTest.StartChromeAsync<ClickTest, AssemblyControlTypeProvider>();

        await ctx.GetRequiredElement(ctx.Control.btnSetResult).ClickAsync();

        Assert.Equal("Success", ctx.GetRequiredElement(ctx.Control.lblResult).Text);
    }
}
