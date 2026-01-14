using WebFormsCore.UI.WebControls;
using DropDownPage = WebFormsCore.Tests.Controls.DropDown.Pages.DropDownPage;
using System;

namespace WebFormsCore.Tests.Controls.DropDown;

public class DropDownTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task DynamicForm(Browser type)
    {
        await using var result = await fixture.StartAsync<DropDownPage>(type);

        Assert.Equal(0, result.Control.EventCount);
        Assert.Equal("", result.Control.ddl.SelectedValue);

        await result.Control.ddl.SelectAsync("1");

        Assert.Equal(1, result.Control.EventCount);
        Assert.Equal("1", result.Control.ddl.SelectedValue);

        await result.Control.btn.ClickAsync();

        Assert.Equal(0, result.Control.EventCount);
        Assert.Equal("1", result.Control.ddl.SelectedValue);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ListItemAttributes(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new DropDownList
        {
            ID = "ddl",
            Items =
            {
                new ListItem("Item 1", "1") { Attributes = { ["data-test"] = "val1" } },
                new ListItem("Item 2", "2") { Enabled = false }
            }
        });

        var option1 = result.Browser.QuerySelector("option[value='1']");
        var option2 = result.Browser.QuerySelector("option[value='2']");

        Assert.NotNull(option1);
        Assert.NotNull(option2);
        Assert.Equal("val1", await option1.GetAttributeAsync("data-test"));
        Assert.Equal("true", await option2.GetAttributeAsync("disabled"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task SelectionProperties(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new DropDownList
        {
            ID = "ddl",
            Items =
            {
                new ListItem("Item 1", "1"),
                new ListItem("Item 2", "2")
            }
        });

        // Test SelectedIndex setter
        result.State.SelectedIndex = 1;
        Assert.Equal("2", result.State.SelectedValue);
        Assert.True(result.State.Items[1].Selected);
        Assert.False(result.State.Items[0].Selected);

        // Test SelectedValue setter
        result.State.SelectedValue = "1";
        Assert.Equal(0, result.State.SelectedIndex);
        Assert.True(result.State.Items[0].Selected);
        Assert.False(result.State.Items[1].Selected);

        // Test out of range
        Assert.Throws<ArgumentOutOfRangeException>(() => result.State.SelectedIndex = 5);

        // Test ClearSelection
        result.State.ClearSelection();
        Assert.False(result.State.Items[0].Selected);
        Assert.Equal(0, result.State.SelectedIndex);
        Assert.Equal("1", result.State.SelectedValue);
    }
}
