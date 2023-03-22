using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebFormsCore.Options;
using WebFormsCore.Tests.Pages;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

[UsesVerify]
public class PageTest
{
    [Fact]
    public async Task PageWithControl()
    {
        var services = new ServiceCollection();

        services.AddWebFormsInternals();
        services.AddWebFormsControlCompiler();
        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options => options.Enabled = false);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var stream = new MemoryStream();

        DisposableControl[] controls;

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var pageManager = serviceProvider.GetRequiredService<IPageManager>();

            var coreRequest = new Mock<IHttpRequest>();
            coreRequest.SetupGet(x => x.Method).Returns("GET");

            var coreResponse = new Mock<IHttpResponse>();
            var headers = new HeaderDictionary();
            coreResponse.SetupGet(x => x.Headers).Returns(headers);
            coreResponse.SetupGet(x => x.Body).Returns(stream);

            var coreContext = new Mock<IHttpContext>();
            coreContext.SetupGet(c => c.Request).Returns(coreRequest.Object);
            coreContext.SetupGet(c => c.Response).Returns(coreResponse.Object);

            var page = await pageManager.RenderPageAsync(coreContext.Object,  "Pages/Page.aspx");

            controls = page.EnumerateControls().OfType<DisposableControl>().ToArray();

            Assert.Equal(2, controls.Length);
            Assert.False(controls[0].IsDisposed, "Control in Page should not be disposed");
            Assert.False(controls[1].IsDisposed, "Dynamic control should not be disposed");
        }

        Assert.True(controls[0].IsDisposed, "Control in Page should be disposed");
        Assert.True(controls[1].IsDisposed, "Dynamic control should be disposed");

        stream.Position = 0;

        var html = Encoding.UTF8.GetString(stream.ToArray());

        await Verify(html);
    }

    public class TestEnvironment : IWebFormsEnvironment
    {
        public string? ContentRootPath => AppContext.BaseDirectory;

        public bool EnableControlWatcher => false;
    }

}
