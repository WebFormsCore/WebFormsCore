using WebFormsCore.Tests.Controls.Checkboxes.Pages;

namespace WebFormsCore.Tests.Controls.Checkboxes;

public class CheckboxTest
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task OnCheckedChanged(Browser type)
    {
        await using var result = await StartAsync<CheckboxPostbackPage>(type);

        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.button.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.button.ClickAsync();
        Assert.Equal("Unchecked", result.Control.label.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AutoPostback_OnCheckedChanged(Browser type)
    {
        await using var result = await StartAsync<CheckboxAutoPostbackPage>(type);

        Assert.Equal("Unchanged", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Checked", result.Control.label.FindBrowserElement().Text);

        await result.Control.checkbox.ClickAsync();
        Assert.Equal("Unchecked", result.Control.label.FindBrowserElement().Text);
    }
}
