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
        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options => options.Enabled = false);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var stream = new MemoryStream();

        DisposableControl? control;

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var controlManager = serviceProvider.GetRequiredService<IControlManager>();

            var featureCollection = new FeatureCollection();

            var coreRequest = new Mock<HttpRequest>();
            coreRequest.SetupGet(x => x.Method).Returns("GET");

            var coreResponse = new Mock<HttpResponse>();
            var headers = new HeaderDictionary();
            coreResponse.SetupGet(x => x.Headers).Returns(headers);
            coreResponse.SetupGet(x => x.Body).Returns(stream);

            var coreContext = new Mock<HttpContext>();
            coreContext.SetupGet(c => c.Request).Returns(coreRequest.Object);
            coreContext.SetupGet(c => c.Response).Returns(coreResponse.Object);
            coreContext.Setup(c => c.Features).Returns(featureCollection);

            var page = await controlManager.RenderPageAsync(
                coreContext.Object,
                scope.ServiceProvider,
                "Pages/Page.aspx",
                stream,
                CancellationToken.None
            );

            control = page.FindControl("control") as DisposableControl;

            Assert.NotNull(control);
            Assert.False(control.IsDisposed);
        }

        Assert.True(control.IsDisposed);

        stream.Position = 0;

        var html = Encoding.UTF8.GetString(stream.ToArray());

        await Verify(html);
    }

    public class TestEnvironment : IWebFormsEnvironment
    {
        public string ContentRootPath => AppContext.BaseDirectory;

        public bool EnableControlWatcher => false;
    }

}
