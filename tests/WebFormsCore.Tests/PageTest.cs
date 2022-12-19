using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebFormsCore.Options;

namespace WebFormsCore.Tests;

[UsesVerify]
public class PageTest
{
    [Theory]
    [InlineData("Pages/PageWithCSharpCode.aspx")]
    public async Task PageWithControl(string pagePath)
    {
        var services = new ServiceCollection();

        services.AddWebFormsInternals();
        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options => options.Enabled = false);

        await using var serviceProvider = services.BuildServiceProvider();

        var page = serviceProvider.GetRequiredService<IControlManager>();

        var featureCollection = new FeatureCollection();
        await using var stream = new MemoryStream();

        var coreRequest = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();
        coreRequest.SetupGet(x => x.Method).Returns("GET");

        var coreResponse = new Mock<Microsoft.AspNetCore.Http.HttpResponse>();
        var headers = new HeaderDictionary();
        coreResponse.SetupGet(x => x.Headers).Returns(headers);
        coreResponse.SetupGet(x => x.Body).Returns(stream);

        var coreContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
        coreContext.SetupGet(c => c.Request).Returns(coreRequest.Object);
        coreContext.SetupGet(c => c.Response).Returns(coreResponse.Object);
        coreContext.Setup(c => c.Features).Returns(featureCollection);

        await page.RenderPageAsync(
            coreContext.Object,
            serviceProvider,
            pagePath,
            stream,
            CancellationToken.None
        );

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
