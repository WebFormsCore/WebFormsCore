using WebFormsCore.Tests.Pages;
using WebFormsCore.UI.WebControls;
using Repeater = WebFormsCore.UI.WebControls.Repeater;

namespace WebFormsCore.Tests;

public class RepeaterTests
{
    [Fact]
    public async Task TypedRepeater()
    {
        await using var result = await RenderAsync<TypedRepeaterPage>();

        Assert.IsType<Repeater<RepeaterDataItem>>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Fact]
    public async Task Repeater()
    {
        await using var result = await RenderAsync<RepeaterPage>();

        Assert.IsType<Repeater>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }
}
