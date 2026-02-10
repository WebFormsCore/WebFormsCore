using WebFormsCore.UI;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Forms;

public class ScopedFormTests(SeleniumFixture fixture)
{
    /// <summary>
    /// Basic postback within a scoped form should work correctly.
    /// </summary>
    [Theory, ClassData(typeof(BrowserData))]
    public async Task BasicScopedFormPostback(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            return new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "count: 0"
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) =>
                        {
                            var current = int.Parse(label.Value.Text.Split(": ")[1]);
                            label.Value.Text = $"count: {current + 1}";
                        }
                    }
                ]
            };
        });

        Assert.Equal("count: 0", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("count: 1", result.Browser.QuerySelector("span")?.Text);

        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("count: 2", result.Browser.QuerySelector("span")?.Text);
    }

    /// <summary>
    /// Two sibling scoped forms: clicking in one form doesn't affect the other form's state.
    /// </summary>
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ScopedFormsViewStateIsolation(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var labelA = new Ref<Label>();
            var labelB = new Ref<Label>();

            var formA = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = labelA,
                        Text = "A: 0"
                    },
                    new Button
                    {
                        Text = "Click A",
                        OnClick = (_, _) =>
                        {
                            var current = int.Parse(labelA.Value.Text.Split(": ")[1]);
                            labelA.Value.Text = $"A: {current + 1}";
                        }
                    }
                ]
            };

            var formB = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = labelB,
                        Text = "B: 0"
                    },
                    new Button
                    {
                        Text = "Click B",
                        OnClick = (_, _) =>
                        {
                            var current = int.Parse(labelB.Value.Text.Split(": ")[1]);
                            labelB.Value.Text = $"B: {current + 1}";
                        }
                    }
                ]
            };

            return new Panel
            {
                Controls = [formA, formB]
            };
        });

        // Both start at 0
        var spans = new List<IElement>();

        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        Assert.Equal("A: 0", spans[0].Text);
        Assert.Equal("B: 0", spans[1].Text);

        // Click button in Form A
        var buttons = new List<IElement>();
        await foreach (var el in result.Browser.QuerySelectorAll("button"))
        {
            buttons.Add(el);
        }

        await buttons[0].ClickAsync();

        // Refresh element references after postback
        spans.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        // Form A updated, Form B unchanged
        Assert.Equal("A: 1", spans[0].Text);
        Assert.Equal("B: 0", spans[1].Text);

        // Click button in Form B
        buttons.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("button"))
        {
            buttons.Add(el);
        }

        await buttons[1].ClickAsync();

        // Refresh element references
        spans.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        // Form A still at 1, Form B updated to 1
        Assert.Equal("A: 1", spans[0].Text);
        Assert.Equal("B: 1", spans[1].Text);
    }

    /// <summary>
    /// Two sibling scoped forms can process concurrent postbacks without blocking each other.
    /// Form B completes while Form A is still blocked on the server.
    /// </summary>
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ConcurrentScopedFormPostbacks(Browser type)
    {
        var requestBlockerA = new TaskCompletionSource();
        var requestStartedA = new TaskCompletionSource();

        await using var result = await fixture.StartAsync(type, () =>
        {
            var labelA = new Ref<Label>();
            var labelB = new Ref<Label>();

            var formA = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = labelA,
                        Text = "A: waiting"
                    },
                    new Button
                    {
                        Text = "Click A",
                        OnClickAsync = async (_, _) =>
                        {
                            requestStartedA.TrySetResult();
                            await Task.WhenAny(requestBlockerA.Task, Task.Delay(10000));
                            labelA.Value.Text = "A: done";
                        }
                    }
                ]
            };

            var formB = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = labelB,
                        Text = "B: waiting"
                    },
                    new Button
                    {
                        Text = "Click B",
                        OnClick = (_, _) =>
                        {
                            labelB.Value.Text = "B: done";
                        }
                    }
                ]
            };

            return new Panel
            {
                Controls = [formA, formB]
            };
        });

        // Trigger Form A's postback via JS (non-blocking, no WaitForPostBack)
        await result.Browser.ExecuteScriptAsync("document.querySelectorAll('button')[0].click()");

        // Wait until Form A's handler is actually executing on the server
        await Task.WhenAny(requestStartedA.Task, Task.Delay(5000));
        Assert.True(requestStartedA.Task.IsCompleted, "Form A handler should have started");

        // Trigger Form B's postback via JS — should NOT be blocked by Form A
        await result.Browser.ExecuteScriptAsync("document.querySelectorAll('button')[1].click()");

        // Poll until Form B completes (it should complete quickly since it's not blocked)
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        string? textB = null;

        while (DateTime.UtcNow < deadline)
        {
            textB = result.Browser.QuerySelector("[data-wfc-form='self']:last-child span")?.Text;

            if (textB == "B: done")
                break;

            await Task.Delay(100);
        }

        Assert.Equal("B: done", textB);

        // Form A should still be waiting
        var textA = result.Browser.QuerySelector("[data-wfc-form='self']:first-child span")?.Text;
        Assert.Equal("A: waiting", textA);

        // Release Form A
        requestBlockerA.SetResult();

        // Poll until Form A completes
        deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        while (DateTime.UtcNow < deadline)
        {
            textA = result.Browser.QuerySelector("[data-wfc-form='self']:first-child span")?.Text;

            if (textA == "A: done")
                break;

            await Task.Delay(100);
        }

        Assert.Equal("A: done", textA);
    }

    /// <summary>
    /// Nested scoped forms: both parent and child work correctly when used sequentially.
    /// The child form operates within the parent's scope.
    /// </summary>
    [Theory, ClassData(typeof(BrowserData))]
    public async Task NestedScopedForms(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var parentLabel = new Ref<Label>();
            var childLabel = new Ref<Label>();

            var childForm = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = childLabel,
                        Text = "child: 0"
                    },
                    new Button
                    {
                        Text = "Click Child",
                        OnClick = (_, _) =>
                        {
                            var current = int.Parse(childLabel.Value.Text.Split(": ")[1]);
                            childLabel.Value.Text = $"child: {current + 1}";
                        }
                    }
                ]
            };

            var parentForm = new HtmlForm
            {
                Scoped = true,
                Controls =
                [
                    new Label
                    {
                        Ref = parentLabel,
                        Text = "parent: 0"
                    },
                    new Button
                    {
                        Text = "Click Parent",
                        OnClick = (_, _) =>
                        {
                            var current = int.Parse(parentLabel.Value.Text.Split(": ")[1]);
                            parentLabel.Value.Text = $"parent: {current + 1}";
                        }
                    },
                    childForm
                ]
            };

            return parentForm;
        });

        // Initial state
        var spans = new List<IElement>();
        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        Assert.Equal("parent: 0", spans[0].Text);
        Assert.Equal("child: 0", spans[1].Text);

        // Click parent button
        var buttons = new List<IElement>();
        await foreach (var el in result.Browser.QuerySelectorAll("button"))
        {
            buttons.Add(el);
        }

        await buttons[0].ClickAsync();

        spans.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        Assert.Equal("parent: 1", spans[0].Text);
        Assert.Equal("child: 0", spans[1].Text);

        // Click child button
        buttons.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("button"))
        {
            buttons.Add(el);
        }

        await buttons[1].ClickAsync();

        spans.Clear();
        await foreach (var el in result.Browser.QuerySelectorAll("span"))
        {
            spans.Add(el);
        }

        Assert.Equal("parent: 1", spans[0].Text);
        Assert.Equal("child: 1", spans[1].Text);
    }
}
