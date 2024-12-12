using WebFormsCore.Tests.Controls.Forms.Pages;

namespace WebFormsCore.Tests.Forms;

public class FormControlTests(SeleniumFixture fixture) : IClassFixture<SeleniumFixture>
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task DynamicForm(Browser type)
    {
        await using var result = await fixture.StartAsync<DynamicForms>(type);

        await result.Control.btnSubmit.ClickAsync();

        await result.QuerySelectorRequired("#dynamicButton").ClickAsync();

        Assert.Equal("Clicked", result.QuerySelectorRequired("#dynamicButton").Text);

        await Task.Yield();
    }
}
