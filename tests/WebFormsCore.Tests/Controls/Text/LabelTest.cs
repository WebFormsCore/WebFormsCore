using WebFormsCore.UI.WebControls;
using WebFormsCore.UI;

namespace WebFormsCore.Tests.Controls.Text;

public class LabelTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderBasic(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Label
        {
            ID = "label",
            Text = "Hello World"
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("Hello World", element.Text);
        Assert.Equal("span", (await element.GetAttributeAsync("tagName"))?.ToLower());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderAssociatedControl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var label = new Label
            {
                ID = "label",
                Text = "Username",
                AssociatedControlID = "tb"
            };
            var tb = new TextBox { ID = "tb" };
            var panel = new Panel { Controls = [label, tb] };

            container.Controls = [panel];

            return (panel, label, tb);
        });

        var labelElement = result.State.label.FindBrowserElement();
        var tbElement = result.State.tb.FindBrowserElement();

        Assert.NotNull(labelElement);
        Assert.NotNull(tbElement);
        Assert.Equal(await tbElement.GetAttributeAsync("id"), await labelElement.GetAttributeAsync("for"));
        Assert.Equal("label", (await labelElement.GetAttributeAsync("tagName"))?.ToLower());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RenderWithControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Label
        {
            ID = "label",
            Controls = [new Literal { Text = "<b>Bold</b>", Mode = LiteralMode.PassThrough }]
        });

        var element = result.State.FindBrowserElement();

        Assert.NotNull(element);
        Assert.Equal("Bold", element.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AssociatedControlNotFound(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Label
        {
            ID = "label",
            Text = "Username",
            AssociatedControlID = "nonexistent"
        });

        var labelElement = result.State.FindBrowserElement();

        Assert.NotNull(labelElement);
        Assert.Equal("nonexistent", await labelElement.GetAttributeAsync("for"));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControlReset(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Label
        {
            ID = "label",
            Text = "Initial Text",
            AssociatedControlID = "test"
        });

        // Clear the control
        result.State.ClearControl();

        Assert.Equal(string.Empty, result.State.Text);
        Assert.Null(result.State.AssociatedControlID);
    }
}
