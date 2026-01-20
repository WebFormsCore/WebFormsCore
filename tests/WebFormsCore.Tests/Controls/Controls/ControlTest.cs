using WebFormsCore.Tests.Controls.Pages;

namespace WebFormsCore.Tests.Controls;

public class ControlTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task CustomClientId(Browser type)
    {
        await using var result = await fixture.StartAsync<ClientIdPage>(type);

        Assert.Equal("clientId", await result.Control.serverId.GetBrowserAttributeAsync("id"));
        await result.Control.serverId.TypeAsync("Success");

        await result.Control.btnSubmit.ClickAsync();

        Assert.Equal("clientId", await result.Control.serverId.GetBrowserAttributeAsync("id"));
        Assert.Equal("Success", result.Control.serverId.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task LoadOrder(Browser type)
    {
        await using var result = await fixture.StartAsync<LoadOrder>(type);

        Assert.Equal("Success", result.Control.loadControl.lbl.GetBrowserText());
    }
}
