using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.RequiredFieldValidators;

public class ValidatorPropertyTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ErrorMessageAndText(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var tb = new TextBox { ID = "tb" };
            var rfv = new RequiredFieldValidator
            {
                ID = "rfv",
                ControlToValidate = "tb",
                ErrorMessage = "Error message",
                Text = "Simple text",
            };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [tb, rfv, btn] };

            container.Controls = [panel];

            return (panel, rfv, btn);
        });

        await result.State.btn.ClickAsync(); // Trigger validation

        var element = result.State.rfv.FindBrowserElement();
        Assert.Equal("Simple text", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task FormattableErrorMessage(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var tb = new TextBox { ID = "tb" };
            var rfv = new RequiredFieldValidator
            {
                ID = "rfv",
                ControlToValidate = "tb",
                ErrorMessage = "Error message",
            };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [tb, rfv, btn] };

            container.Controls = [panel];

            return (panel, rfv, btn);
        });

        await result.State.btn.ClickAsync(); // Trigger validation

        var element = result.State.rfv.FindBrowserElement();
        Assert.Equal("Error message", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Enabled_IsValid(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var tb = new TextBox { ID = "tb" };
            var rfv = new RequiredFieldValidator { ID = "rfv", ControlToValidate = "tb", IsValid = false };
            var panel = new Panel { Controls = [tb, rfv] };

            container.Controls = [panel];

            return (panel, rfv);
        });

        Assert.False(result.State.rfv.IsValid);
        
        result.State.rfv.Enabled = false;
        Assert.True(result.State.rfv.IsValid); // Setting Enabled=false should set IsValid=true
    }
}
