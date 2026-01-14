using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.ViewState;

public class ViewStatePolymorphismTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ViewState_Polymorphism(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var custom = new PolymorphicControl { ID = "custom", Value = "Test String" };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [custom, btn] };

            container.Controls = [panel];

            return (panel, custom, btn);
        });

        Assert.Equal("Test String", result.State.custom.Value);

        await result.State.btn.ClickAsync();

        Assert.Equal("Test String", result.State.custom.Value);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ViewState_Polymorphism_Int(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var custom = new PolymorphicControl { ID = "custom", Value = 123 };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [custom, btn] };

            container.Controls = [panel];

            return (panel, custom, btn);
        });

        Assert.Equal(123, result.State.custom.Value);

        await result.State.btn.ClickAsync();

        Assert.Equal(123, result.State.custom.Value);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ViewState_List(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var custom = new ListControl { ID = "custom", Values = ["A", "B"] };
            var btn = new Button { ID = "btn" };
            var panel = new Panel { Controls = [custom, btn] };

            container.Controls = [panel];

            return (panel, custom, btn);
        });

        Assert.Equal(["A", "B"], result.State.custom.Values);

        await result.State.btn.ClickAsync();

        Assert.Equal(["A", "B"], result.State.custom.Values);
    }
}

public class PolymorphicControl : Control
{
    public object? Value
    {
        get => ViewState["Value"];
        set => ViewState["Value"] = value;
    }
}

public class ListControl : Control
{
    public List<string>? Values
    {
        get => ViewState["Values"] as List<string>;
        set => ViewState["Values"] = value;
    }
}
