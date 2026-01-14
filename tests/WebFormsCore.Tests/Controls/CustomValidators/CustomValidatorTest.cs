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

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ValidateEmptyValue(Browser type)
    {
        await using var result = await fixture.StartAsync(type, async control =>
        {
            var textBox = new UI.WebControls.TextBox
            {
                ID = "textBox"
            };

            var validator = new UI.WebControls.CustomValidator
            {
                ID = "validator",
                ControlToValidate = "textBox",
                ErrorMessage = "Invalid",
                ValidateEmptyText = true
            };

            validator.ServerValidate += (_, args) =>
            {
                args.IsValid = !string.IsNullOrEmpty(args.Value);
                return Task.CompletedTask;
            };

            var button = new UI.WebControls.Button
            {
                ID = "button",
                Text = "Submit"
            };

            await control.Controls.AddAsync(textBox);
            await control.Controls.AddAsync(validator);
            await control.Controls.AddAsync(button);

            return (textBox, validator, button);
        });

        // Should fail validation with empty value when ValidateEmptyText is true
        await result.State.button.ClickAsync();
        Assert.True(await result.State.validator.FindBrowserElement().IsVisibleAsync());

        // Change to valid value
        await result.State.textBox.TypeAsync("value");
        await result.State.button.ClickAsync();
        Assert.False(await result.State.validator.FindBrowserElement().IsVisibleAsync());
    }
}
