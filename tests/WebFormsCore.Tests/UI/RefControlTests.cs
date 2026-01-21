using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI;

public class RefControlTests(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClickRefCounter(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var count = new Ref<int>();

            return new Panel
            {
                Controls =
                [
                    new Label
                    {
                        Attributes =
                        {
                            { "data-count", () => count.Value.ToString() }
                        },
                        Controls =
                        [
                            new Literal(() => $"Count: {count.Value}")
                        ]
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) => count.Value++,
                    }
                ]
            };
        });

        Assert.Equal("Count: 0", result.Browser.QuerySelector("span")?.Text);
        Assert.Equal("0", await result.Browser.QuerySelector("span")!.GetAttributeAsync("data-count"));
        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Count: 1", result.Browser.QuerySelector("span")?.Text);
        Assert.Equal("1", await result.Browser.QuerySelector("span")!.GetAttributeAsync("data-count"));
        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Count: 2", result.Browser.QuerySelector("span")?.Text);
        Assert.Equal("2", await result.Browser.QuerySelector("span")!.GetAttributeAsync("data-count"));
    }

    [Theory, CombinatorialData]
    public async Task ClickRefCounterMultiple(Browser type, [CombinatorialValues(0, 1, 5, 10, 20, 40)] int count)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var counters = new Ref<int[]>(new int[count]);
            var controls = new List<Panel>();

            for (int i = 0; i < count; i++)
            {
                var index = i;

                controls.Add(new Panel
                {
                    Attributes =
                    {
                        ["data-index"] = index.ToString()
                    },
                    Controls =
                    [
                        new Label
                        {
                            Controls = [new Literal(() => $"{counters.Value[index]}")]
                        },
                        new Button
                        {
                            Text = "Increment",
                            OnClick = (_, _) =>
                            {
                                counters.Value[index]++;
                            }
                        }
                    ]
                });
            }

            return new Panel
            {
                Controls =
                [
                    ..controls
                ]
            };
        });

        await ValidateAsync(0);
        await ValidateAsync(1);
        await ValidateAsync(2);
        return;

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        async Task ValidateAsync(int expected)
        {
            var before = expected.ToString();
            var after = (expected + 1).ToString();

            for (var i = 0; i < count; i++)
            {
                var label = result.Browser.QuerySelector($"div[data-index='{i}'] span")!;
                var button = result.Browser.QuerySelector($"div[data-index='{i}'] button");

                Assert.Equal(before, label.Text);
                await button!.ClickAsync();
                Assert.Equal(after, label.Text);
            }
        }
    }
}
