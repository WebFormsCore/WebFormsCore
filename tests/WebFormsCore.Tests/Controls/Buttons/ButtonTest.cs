using WebFormsCore.Tests.Controls.Buttons.Pages;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Buttons;


public class ButtonTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task Click(Browser type)
    {
        await using var result = await fixture.StartAsync<ClickTest>(type);

        Assert.Empty(result.Control.lblResult.Text);

        await result.Control.btnSetResult.ClickAsync();

        Assert.Equal("Success", result.Control.lblResult.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClickRef(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            return new Panel
            {
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "Not clicked"
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) => label.Value.Text = "Clicked",
                    }
                ]
            };
        });

        Assert.Equal("Not clicked", result.Browser.QuerySelector("span")?.Text);
        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Clicked", result.Browser.QuerySelector("span")?.Text);
    }
}
