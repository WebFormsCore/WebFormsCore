using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls;

public class ControlCollectionTest(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task AddControl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls = [new Label { ID = "label", Text = "Test" }]
        });

        Assert.Single(result.State.Controls);
        Assert.True(result.State.HasControls());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task AddMultipleControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Label { Text = "Label1" },
                new Label { Text = "Label2" },
                new Label { Text = "Label3" }
            ]
        });

        Assert.Equal(3, result.State.Controls.Count);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task IndexerAccess(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Label { ID = "label1", Text = "First" },
                new Label { ID = "label2", Text = "Second" }
            ]
        });

        Assert.Equal("label1", result.State.Controls[0].ID);
        Assert.Equal("label2", result.State.Controls[1].ID);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task RemoveControl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var label = new Label { ID = "label", Text = "Test" };
            var panel = new Panel { ID = "panel", Controls = [label] };

            container.Controls = [panel];

            return (panel, label);
        });

        Assert.Single(result.State.panel.Controls);

        result.State.panel.Controls.Remove(result.State.label);

        Assert.Empty(result.State.panel.Controls);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClearControls(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Label { Text = "Label1" },
                new Label { Text = "Label2" },
                new Label { Text = "Label3" }
            ]
        });

        Assert.Equal(3, result.State.Controls.Count);

        result.State.Controls.Clear();

        Assert.Empty(result.State.Controls);
        Assert.False(result.State.HasControls());
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task ContainsControl(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var label = new Label { ID = "label", Text = "Test" };
            var panel = new Panel { ID = "panel", Controls = [label] };

            container.Controls = [panel];

            return (panel, label);
        });

        Assert.True(result.State.panel.Controls.Contains(result.State.label));

        var otherLabel = new Label { ID = "other" };
        Assert.False(result.State.panel.Controls.Contains(otherLabel));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task IndexOf(Browser type)
    {
        await using var result = await fixture.StartAsync(type, container =>
        {
            var label1 = new Label { ID = "label1", Text = "First" };
            var label2 = new Label { ID = "label2", Text = "Second" };
            var panel = new Panel { ID = "panel", Controls = [label1, label2] };

            container.Controls = [panel];

            return (panel, label1, label2);
        });

        Assert.Equal(0, result.State.panel.Controls.IndexOf(result.State.label1));
        Assert.Equal(1, result.State.panel.Controls.IndexOf(result.State.label2));
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task Enumerate(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () => new Panel
        {
            ID = "panel",
            Controls =
            [
                new Label { ID = "label1", Text = "First" },
                new Label { ID = "label2", Text = "Second" },
                new Label { ID = "label3", Text = "Third" }
            ]
        });

        var count = 0;
        foreach (Control ctrl in result.State.Controls)
        {
            Assert.NotNull(ctrl.ID);
            count++;
        }

        Assert.Equal(3, count);
    }
}
