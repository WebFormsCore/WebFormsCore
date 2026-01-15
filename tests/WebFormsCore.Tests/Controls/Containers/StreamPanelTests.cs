using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore.Tests.Controls.Containers;

public class StreamPanelTests(SeleniumFixture fixture)
{
    [Theory, ClassData(typeof(BrowserData))]
    public async Task ClickRefStreaming(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            return new StreamPanel
            {
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "Not clicked"
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) => label.Value.Text = "Clicked",
                    }
                ]
            };
        });

        // Wait for WebSocket to connect
        result.Browser.WaitForStreamPanelConnection();

        Assert.Equal("Not clicked", result.Browser.QuerySelector("span")?.Text);
        await result.Browser.QuerySelector("button")!.ClickAsync();
        Assert.Equal("Clicked", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenWebSocketsDisabledThenStreamPanelRendersButDoesNotUpdate(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            var label = new Ref<Label>();

            return new StreamPanel
            {
                Controls =
                [
                    new Label
                    {
                        Ref = label,
                        Text = "Initial"
                    },
                    new Button
                    {
                        Text = "Click me",
                        OnClick = (_, _) => label.Value.Text = "Updated",
                    }
                ]
            };
        }, new SeleniumFixtureOptions
        {
            EnableWebSockets = false
        });

        // The StreamPanel should render its initial content
        Assert.Equal("Initial", result.Browser.QuerySelector("span")?.Text);

        // The data-wfc-stream attribute should still be rendered
        Assert.NotNull(result.Browser.QuerySelector("[data-wfc-stream]"));

        // Wait for WebSocket to fail to connect
        result.Browser.WaitForStreamPanelDisconnection();

        // When WebSockets middleware is disabled, clicking the button won't trigger updates
        // because the WebSocket connection failed
        await result.Browser.QuerySelector("button")!.ClickAsync();

        // The label should NOT have been updated because WebSocket couldn't connect
        Assert.Equal("Initial", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task WhenAllowStreamPanelDisabledThenThrowsConfigurationException(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new StreamPanel
            {
                Controls =
                [
                    new Label { Text = "Initial" },
                    new Button { Text = "Click me" }
                ]
            };
        }, new SeleniumFixtureOptions
        {
            Configure = services =>
            {
                services.Configure<WebFormsCoreOptions>(options =>
                {
                    options.AllowStreamPanel = false;
                });
            }
        });

        // The StreamPanel should render its initial content
        Assert.Equal("Initial", result.Browser.QuerySelector("span")?.Text);

        // Wait for the WebSocket connection attempt to be rejected
        result.Browser.WaitForStreamPanelDisconnection();

        // The server should have recorded an exception
        var exception = result.Browser.LastException;
        Assert.NotNull(exception);
        Assert.IsType<StreamPanelConfigurationException>(exception);
        Assert.Contains("AllowStreamPanel", exception.Message);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task StreamPanelRendersChildrenOnInitialLoad(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new StreamPanel
            {
                ID = "streamPanel",
                Controls =
                [
                    new Label { Text = "Child Content" }
                ]
            };
        });

        // The StreamPanel should render the div with id and data-wfc-stream attribute
        var panel = result.Browser.QuerySelector("[data-wfc-stream]");
        Assert.NotNull(panel);

        // The child content should be rendered
        Assert.Equal("Child Content", result.Browser.QuerySelector("span")?.Text);
    }

    [Theory, ClassData(typeof(BrowserData))]
    public async Task StreamPanelWithPrerenderTrue(Browser type)
    {
        await using var result = await fixture.StartAsync(type, () =>
        {
            return new StreamPanel
            {
                Prerender = true,
                Controls =
                [
                    new Label { Text = "Prerendered Content" }
                ]
            };
        });

        // Content should be visible
        Assert.Equal("Prerendered Content", result.Browser.QuerySelector("span")?.Text);
    }
}

