using WebFormsCore.Tests.Controls.Pages;

namespace WebFormsCore.Tests.Controls;

public class ControlTest
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task CustomClientId(Browser type)
    {
        await using var result = await StartAsync<ClientIdPage>(type);

        Assert.Equal("clientId", await result.Control.serverId.GetBrowserAttributeAsync("id"));
        await result.Control.serverId.TypeAsync("Success");

        await result.Control.btnSubmit.ClickAsync();

        Assert.Equal("clientId", await result.Control.serverId.GetBrowserAttributeAsync("id"));
        Assert.Equal("Success", result.Control.serverId.Text);
    }
}
