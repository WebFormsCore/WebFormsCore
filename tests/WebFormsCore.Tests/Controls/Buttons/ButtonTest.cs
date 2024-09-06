using WebFormsCore.Tests.Controls.Buttons.Pages;

namespace WebFormsCore.Tests.Controls.Buttons;

public class ButtonTest
{
    [Fact]
    public async Task Click()
    {
        await using var result = await StartAsync<ClickTest>();

        Assert.Empty(result.Control.lblResult.Text);

        await result.GetRequiredElement(result.Control.btnSetResult).ClickAsync();

        Assert.Equal("Success", result.GetRequiredElement(result.Control.lblResult).Text);
    }
}
