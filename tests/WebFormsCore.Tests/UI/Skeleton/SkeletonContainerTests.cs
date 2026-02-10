using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;
using WebFormsCore.UI.Skeleton;
using WebFormsCore.UI.WebControls;
using Xunit;

namespace WebFormsCore.Tests.UnitTests.UI.Skeleton;

public class SkeletonContainerTests
{
    private Page CreatePage(bool withSkeletonSupport = true)
    {
        var services = new ServiceCollection();
        var builder = services.AddWebFormsCore();

        if (withSkeletonSupport)
        {
            builder.AddSkeletonSupport();
        }

        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var page = new Page();
        ((IInternalPage)page).SetContext(httpContext);
        return page;
    }

    [Fact]
    public async Task WhenLoadingFalseThenRendersChildrenNormally()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = false };
        page.Controls.AddWithoutPageEvents(container);

        var label = new Label { Text = "Hello" };
        container.Controls.AddWithoutPageEvents(label);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("Hello", output);
        Assert.DoesNotContain("wfc-skeleton", output);
        Assert.DoesNotContain("data-wfc-skeleton", output);
    }

    [Fact]
    public async Task WhenLoadingTrueThenRendersSkeletonPlaceholders()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        // Label with text renders the text (not skeleton) because static text should be shown
        var label = new Label { Text = "Hello" };
        container.Controls.AddWithoutPageEvents(label);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        // Labels with text render normally in skeleton mode
        Assert.Contains("Hello", output);
        Assert.Contains("data-wfc-skeleton-container", output);
        Assert.Contains("aria-busy=\"true\"", output);
    }

    [Fact]
    public async Task WhenLoadingTrueThenAddsContainerAttributes()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("data-wfc-skeleton-container", output);
        Assert.Contains("aria-busy=\"true\"", output);
    }

    [Fact]
    public async Task WhenLoadingCssClassSetThenAddsClass()
    {
        var page = CreatePage();
        var container = new SkeletonContainer
        {
            Loading = true,
            LoadingCssClass = "is-loading"
        };
        page.Controls.AddWithoutPageEvents(container);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("is-loading", output);
    }

    [Fact]
    public async Task WhenLiteralControlChildThenRendersAsIs()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        var literal = new LiteralControl { Text = "<hr />" };
        container.Controls.AddWithoutPageEvents(literal);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("<hr />", output);
    }

    [Fact]
    public async Task WhenNoSkeletonSupportThenRendersGenericSkeleton()
    {
        var page = CreatePage(withSkeletonSupport: false);
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        // Falls back to generic skeleton div
        Assert.Contains("wfc-skeleton", output);
        Assert.Contains("&nbsp;", output);
    }

    [Fact]
    public async Task WhenChildNotVisibleThenSkipsIt()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        var label = new Label { Text = "Hidden", Visible = false };
        container.Controls.AddWithoutPageEvents(label);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.DoesNotContain("Hidden", output);
        // The container has data-wfc-skeleton-container but no child skeleton elements
        Assert.DoesNotContain("aria-hidden=\"true\"", output);
    }

    [Fact]
    public async Task WhenButtonChildThenRendersDisabledButtonSkeleton()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        // Button with text renders the text (not skeleton placeholder)
        var button = new Button { Text = "Submit" };
        container.Controls.AddWithoutPageEvents(button);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        // Buttons with text render their text but are disabled
        Assert.Contains("Submit", output);
        Assert.Contains("disabled=\"disabled\"", output);
        Assert.Contains("data-wfc-skeleton", output);
    }

    [Fact]
    public async Task WhenTextBoxChildThenRendersDisabledInputSkeleton()
    {
        var page = CreatePage();
        var container = new SkeletonContainer { Loading = true };
        page.Controls.AddWithoutPageEvents(container);

        var textBox = new TextBox { Text = "Input value" };
        container.Controls.AddWithoutPageEvents(textBox);

        var writer = new StringHtmlTextWriter();
        await container.RenderAsync(writer, CancellationToken.None);
        await writer.FlushAsync();

        var output = writer.ToString();
        Assert.Contains("wfc-skeleton-input", output);
        Assert.Contains("disabled=\"disabled\"", output);
    }

    [Fact]
    public void WhenClearControlThenResetsProperties()
    {
        var container = new SkeletonContainer
        {
            Loading = true,
            LoadingCssClass = "loading"
        };

        container.ClearControl();

        Assert.False(container.Loading);
        Assert.Null(container.LoadingCssClass);
    }
}
