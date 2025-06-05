using DropDownPage = WebFormsCore.Tests.Controls.DropDown.Pages.DropDownPage;

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
}
