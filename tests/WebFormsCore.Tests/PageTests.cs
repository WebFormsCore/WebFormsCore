using System.Text;
using HttpStack;
using HttpStack.Collections;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
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

        var coreRequest = Substitute.For<IHttpRequest>();
        coreRequest.Method.Returns("GET");

        var coreResponse = Substitute.For<IHttpResponse>();
        var headers = new HeaderDictionary();
        coreResponse.Headers.Returns(headers);
        coreResponse.Body.Returns(stream);

        var coreContext = Substitute.For<IHttpContext>();
        coreContext.Request.Returns(coreRequest);
        coreContext.Response.Returns(coreResponse);
        coreContext.RequestServices.Returns(serviceProvider);

        var page = await pageManager.RenderPageAsync(coreContext, path);

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
