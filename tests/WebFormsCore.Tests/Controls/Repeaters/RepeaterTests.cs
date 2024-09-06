using WebFormsCore.TestFramework;
using WebFormsCore.Tests.Controls.Repeaters.Pages;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Repeaters;

public class RepeaterTests
{
    [Theory, ClassData(typeof(TestTypeData))]
    public async Task TypedRepeaterTest(TestType type)
    {
        await using var result = await StartAsync<TypedRepeaterPage>(type);

        Assert.IsType<Repeater<RepeaterDataItem>>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Theory, ClassData(typeof(TestTypeData))]
    public async Task RepeaterTest(TestType type)
    {
        await using var result = await StartAsync<RepeaterPage>(type);

        Assert.IsType<Repeater>(result.Control.FindControl("items"));
        Assert.Equal(5, await result.QuerySelectorAll("li").CountAsync());
    }

    [Theory, ClassData(typeof(TestTypeData))]
    public async Task LargeRepeaterTest(TestType type)
    {
        await using var result = await StartAsync<LargeRepeaterPage>(type);

        Assert.Equal(100, await result.QuerySelectorAll("[data-id]").CountAsync());
        await result.QuerySelectorRequired("[data-id='10'] .btn").ClickAsync();

        Assert.Equal("10", result.Control.SelectedId);
    }
}
