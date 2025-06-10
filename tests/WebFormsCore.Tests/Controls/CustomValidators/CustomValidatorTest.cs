using WebFormsCore.Tests.Controls.RequiredFieldValidators.Pages;
using TextBoxPage = WebFormsCore.Tests.Controls.CustomValidators.Pages.TextBoxPage;

namespace WebFormsCore.Tests.Controls.CustomValidators;

public class CustomValidatorTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task Ingore_Without_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Postback without changing the value
        await result.Control.button.ClickAsync();

        // Validator should not be visible
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Equal("Empty", result.Control.labelValue.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Allow_Valid_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Change the value
        await result.Control.textBox.TypeAsync("Changed");
        await result.Control.button.ClickAsync();

        // Validator should not be visible and the label should be filled
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Equal("Changed", result.Control.labelValue.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Disallow_Invalid_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Change the value to an invalid one
        await result.Control.textBox.TypeAsync("invalid");
        await result.Control.button.ClickAsync();

        // Validator should be visible and the label should not be filled
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.True(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Empty(result.Control.labelValue.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Allow_After_Invalid_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Change the value to an invalid one
        await result.Control.textBox.TypeAsync("invalid");
        await result.Control.button.ClickAsync();

        // Validator should be visible and the label should not be filled
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.True(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Empty(result.Control.labelValue.Text);

        // Change the value to a valid one
        await result.Control.textBox.ClearAsync();
        await result.Control.textBox.TypeAsync("valid");
        await result.Control.button.ClickAsync();

        // Validator should not be visible and the label should be filled
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Equal("valid", result.Control.labelValue.Text);
    }
}
