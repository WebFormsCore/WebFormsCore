﻿using WebFormsCore.Tests.Controls.RequiredFieldValidators.Pages;

namespace WebFormsCore.Tests.Controls.RequiredFieldValidators;

public class RequiredFieldValidatorTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task Skip_Without_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Postback without changing the value
        await result.Control.button.ClickAsync();

        // Validator should be visible and the label should not be filled (there should be no postback)
        Assert.Equal("False", result.Control.labelPostback.Text);
        Assert.True(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Empty(result.Control.labelValue.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Submit_With_Value(Browser type)
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
    public async Task Postback_Without_Value(Browser type)
    {
        await using var result = await fixture.StartAsync<TextBoxPage>(type);

        // Validator should not be visible
        Assert.False(await result.Control.validator.FindBrowserElement().IsVisibleAsync());

        // Change the value
        await result.Control.button.PostBackAsync(options: new PostBackOptions
        {
            Validate = false
        });

        // Validator should not be visible and the label should be filled
        Assert.Equal("True", result.Control.labelPostback.Text);
        Assert.True(await result.Control.validator.FindBrowserElement().IsVisibleAsync());
        Assert.Empty(result.Control.labelValue.Text);
    }
}
