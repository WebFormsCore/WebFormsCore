using WebFormsCore.Tests.Controls.Forms.Pages;

namespace WebFormsCore.Tests.Controls.Forms;

public class FormPostbackTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithNoForm(Browser type)
    {
        await using var result = await fixture.StartAsync<NoForm>(type);

        // Initial state
        Assert.Equal("0", result.Control.counter.FindBrowserElement().Text);

        // Click the button
        await result.Control.button.ClickAsync();

        // Counter should increment
        Assert.Equal("1", result.Control.counter.FindBrowserElement().Text);

        // Click again
        await result.Control.button.ClickAsync();

        // Counter should increment again
        Assert.Equal("2", result.Control.counter.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithOneForm(Browser type)
    {
        await using var result = await fixture.StartAsync<OneForm>(type);

        // Initial state
        Assert.Equal("0", result.Control.counter.FindBrowserElement().Text);

        // Click the button
        await result.Control.button.ClickAsync();

        // Counter should increment
        Assert.Equal("1", result.Control.counter.FindBrowserElement().Text);

        // Click again
        await result.Control.button.ClickAsync();

        // Counter should increment again
        Assert.Equal("2", result.Control.counter.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithTwoForms_Form1ButtonClick(Browser type)
    {
        await using var result = await fixture.StartAsync<TwoForms>(type);

        // Initial state - both counters should be 0
        Assert.Equal("0", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.counter2.FindBrowserElement().Text);

        // Click button in form1
        await result.Control.button1.ClickAsync();

        // Only counter1 should increment, counter2 should remain the same
        Assert.Equal("1", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.counter2.FindBrowserElement().Text);

        // Verify button2.UniqueID is not in the postback data
        Assert.DoesNotContain(result.Control.button2.UniqueID, result.HttpContext.Request.Form.Keys);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithTwoForms_Form2ButtonClick(Browser type)
    {
        await using var result = await fixture.StartAsync<TwoForms>(type);

        // Initial state - both counters should be 0
        Assert.Equal("0", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.counter2.FindBrowserElement().Text);

        // Click button in form2
        await result.Control.button2.ClickAsync();

        // Only counter2 should increment, counter1 should remain the same
        Assert.Equal("0", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("1", result.Control.counter2.FindBrowserElement().Text);

        // Verify button1.UniqueID is not in the postback data
        Assert.DoesNotContain(result.Control.button1.UniqueID, result.HttpContext.Request.Form.Keys);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithTwoForms_AlternatingClicks(Browser type)
    {
        await using var result = await fixture.StartAsync<TwoForms>(type);

        // Initial state - both counters should be 0
        Assert.Equal("0", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.counter2.FindBrowserElement().Text);

        // Click button in form1
        await result.Control.button1.ClickAsync();

        // Only counter1 should increment
        Assert.Equal("1", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.counter2.FindBrowserElement().Text);

        // Click button in form2
        await result.Control.button2.ClickAsync();

        // Only counter2 should increment
        Assert.Equal("1", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("1", result.Control.counter2.FindBrowserElement().Text);

        // Click button in form1 again
        await result.Control.button1.ClickAsync();

        // Only counter1 should increment again
        Assert.Equal("2", result.Control.counter1.FindBrowserElement().Text);
        Assert.Equal("1", result.Control.counter2.FindBrowserElement().Text);

        // Verify button2.UniqueID is not in the postback data (last click was form1)
        Assert.DoesNotContain(result.Control.button2.UniqueID, result.HttpContext.Request.Form.Keys);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithNestedForms_DocumentsBehavior(Browser type)
    {
        await using var result = await fixture.StartAsync<NestedForms>(type);

        // Initial state - both counters should be 0
        Assert.Equal("0", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.innerCounter.FindBrowserElement().Text);

        // Verify both forms are registered in Page.Forms
        Assert.Equal(2, result.Control.Forms.Count);
        Assert.Contains(result.Control.outerForm, result.Control.Forms);
        Assert.Contains(result.Control.innerForm, result.Control.Forms);

        // Verify the outer form is the root form
        Assert.True(result.Control.outerForm.IsRootForm);
        Assert.False(result.Control.innerForm.IsRootForm);
        Assert.Equal(result.Control.outerForm, result.Control.innerForm.RootForm);

        // Verify UniqueIDs are properly concatenated for nested controls
        Assert.Equal("outerForm", result.Control.outerForm.UniqueID);
        Assert.Equal("outerForm$innerForm", result.Control.innerForm.UniqueID);
        Assert.Equal("outerForm$innerForm$innerButton", result.Control.innerButton.UniqueID);
        Assert.Equal("outerForm$innerForm$innerCounter", result.Control.innerCounter.UniqueID);

        // Click button in outer form
        await result.Control.outerButton.ClickAsync();

        // Only the outer (root) form renders the wfcForm hidden field
        var wfcFormValue = result.HttpContext.Request.Form["wfcForm"].ToString();
        Assert.Equal(result.Control.outerForm.UniqueID, wfcFormValue);

        // The ActiveForm is the outer (root) form
        Assert.Equal(result.Control.outerForm, result.Control.ActiveForm);

        // Outer button click should increment the outer counter
        Assert.Equal("1", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.innerCounter.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithNestedForms_InnerButtonClick(Browser type)
    {
        await using var result = await fixture.StartAsync<NestedForms>(type);

        // Initial state - both counters should be 0
        Assert.Equal("0", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.innerCounter.FindBrowserElement().Text);

        // Click button in inner form
        await result.Control.innerButton.ClickAsync();

        // Only the outer (root) form renders the wfcForm hidden field
        var wfcFormValue = result.HttpContext.Request.Form["wfcForm"].ToString();
        Assert.Equal(result.Control.outerForm.UniqueID, wfcFormValue);

        // The ActiveForm is the outer (root) form
        Assert.Equal(result.Control.outerForm, result.Control.ActiveForm);

        // Inner button click should increment the inner counter
        Assert.Equal("0", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("1", result.Control.innerCounter.FindBrowserElement().Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task PostbackWithNestedForms_AlternatingClicks(Browser type)
    {
        await using var result = await fixture.StartAsync<NestedForms>(type);

        // Initial state
        Assert.Equal("0", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.innerCounter.FindBrowserElement().Text);

        // Click outer button first
        await result.Control.outerButton.ClickAsync();
        Assert.Equal("1", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("0", result.Control.innerCounter.FindBrowserElement().Text);

        // Verify server-side state is correct after outer click
        Assert.Equal("1", result.Control.outerCounter.Text);
        Assert.Equal("0", result.Control.innerCounter.Text);

        // Now click inner button
        await result.Control.innerButton.ClickAsync();

        // Server-side values should be updated
        Assert.Equal("1", result.Control.outerCounter.Text);
        Assert.Equal("1", result.Control.innerCounter.Text); // This should be "1" after click

        // Browser should also show updated values
        Assert.Equal("1", result.Control.outerCounter.FindBrowserElement().Text);
        Assert.Equal("1", result.Control.innerCounter.FindBrowserElement().Text);
    }
}
