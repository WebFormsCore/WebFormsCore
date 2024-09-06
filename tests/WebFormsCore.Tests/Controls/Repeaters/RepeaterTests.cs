using WebFormsCore.Tests.Controls.Repeaters.Pages;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters;

public class RepeaterTests
{
    [Fact]
    public async Task TypedRepeaterTest()
    {
        await using var result = await StartAsync<TypedRepeaterPage>();

        Assert.IsType<Repeater<RepeaterDataItem>>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Fact]
    public async Task RepeaterTest()
    {
        await using var result = await StartAsync<RepeaterPage>();

        Assert.IsType<Repeater>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Fact]
    public async Task LargeRepeaterTest()
    {
        await using var result = await StartAsync<LargeRepeaterPage>();

        Assert.Equal(100, await result.QuerySelectorAll("[data-id]").CountAsync());
        await result.QuerySelectorRequired("[data-id='10'] .btn").ClickAsync();

        Assert.Equal("10", result.Control.SelectedId);
    }
}
