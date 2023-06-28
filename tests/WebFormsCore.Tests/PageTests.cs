using System.Text;
using HttpStack;
using HttpStack.Collections;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebFormsCore.Options;
using WebFormsCore.UI;

namespace WebFormsCore.Tests;

public partial class PageTest
{
    public static async Task<TestResult> RenderAsync(string path)
    {
        var services = new ServiceCollection();

        services.AddWebForms();
        services.AddWebFormsCompiler();
        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options => options.Enabled = false);

        var serviceProvider = services.BuildServiceProvider();
        await using var stream = new MemoryStream();

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
        coreContext.SetupGet(c => c.RequestServices).Returns(serviceProvider);

        var page = await pageManager.RenderPageAsync(coreContext.Object, path);

        stream.Position = 0;

        var html = Encoding.UTF8.GetString(stream.ToArray());

        return new TestResult(serviceProvider, page, html);
    }

    public sealed class TestResult : IAsyncDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public TestResult(ServiceProvider serviceProvider, Page page, string html)
        {
            _serviceProvider = serviceProvider;
            Page = page;
            Html = html;
        }

        public Page Page { get; }

        public string Html { get;  }

        public ValueTask DisposeAsync()
        {
            return _serviceProvider.DisposeAsync();
        }
    }

    public class TestEnvironment : IWebFormsEnvironment
    {
        public string? ContentRootPath => AppContext.BaseDirectory;

        public bool EnableControlWatcher => false;
    }
}
