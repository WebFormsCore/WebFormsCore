using WebFormsCore.Tests.Controls.Repeaters.Pages;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters;

public class RepeaterTests(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task TypedRepeaterTest(Browser type)
    {
        await using var result = await fixture.StartAsync<TypedRepeaterPage>(type);

        Assert.IsType<Repeater<RepeaterDataItem>>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RepeaterTest(Browser type)
    {
        await using var result = await fixture.StartAsync<RepeaterPage>(type);

        Assert.IsType<Repeater>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LargeRepeaterTest(Browser type)
    {
        await using var result = await fixture.StartAsync<LargeRepeaterPage>(type);

        Assert.Equal(100, await result.QuerySelectorAll("[data-id]").CountAsync());
        await result.QuerySelectorRequired("[data-id='10'] .btn").ClickAsync();

        Assert.Equal("10", result.Control.SelectedId);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LabelInitViewState(Browser type)
    {
        await using var result = await fixture.StartAsync<InitRepeaterViewState>(type);

        Assert.Equal("Success", result.Control.lblViewState.GetBrowserText());
        Assert.Equal(3, await result.QuerySelectorAll(".repeater-label").CountAsync());

        await foreach (var label in result.QuerySelectorAll(".repeater-label"))
        {
            Assert.Equal("Success", label.Text);
        }

        await result.Control.btnSubmit.ClickAsync();

        Assert.Equal("Success", result.Control.lblViewState.GetBrowserText());
        Assert.Equal(3, await result.QuerySelectorAll(".repeater-label").CountAsync());

        await foreach (var label in result.QuerySelectorAll(".repeater-label"))
        {
            Assert.Equal("Success", label.Text);
        }
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task NestedRepeater(Browser type)
    {
        await using var result = await fixture.StartAsync<NestedRepeaterPage>(type);

        Assert.Equal(4, await result.QuerySelectorAll(".repeater-label").CountAsync());

        var values = await result.QuerySelectorAll(".repeater-label").Select(x => x.Text).ToListAsync();

        Assert.Equal([
            "1 - 1",
            "1 - 2",
            "2 - 1",
            "2 - 2"
        ], values);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PredictableRepeaterId(Browser type)
    {
        await using var result = await fixture.StartAsync<PredictableRepeaterId>(type);

        var clientId = result.Control.items.Items[0].FindControl("lbl")?.ClientID;

        Assert.NotNull(clientId);
        Assert.Equal(clientId, result.QuerySelector(".result")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RepeaterTemplateTest(Browser type)
    {
        await using var result = await fixture.StartAsync<RepeaterTemplatePage>(type);

        Assert.Equal("Header", result.QuerySelector("header")?.Text);
        Assert.Equal("Footer", result.QuerySelector("footer")?.Text);
        Assert.Equal(3, await result.QuerySelectorAll(".item").CountAsync());
        Assert.Equal(2, await result.QuerySelectorAll("hr").CountAsync());

        var items = await result.QuerySelectorAll(".item").Select(x => x.Text).ToListAsync();
        Assert.Equal(["Item 1", "Item 2", "Item 3"], items);
    }
}
