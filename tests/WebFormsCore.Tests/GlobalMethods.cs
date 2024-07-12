global using static WebFormsCore.Tests.TestUtils;

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace WebFormsCore.Tests;

internal static class TestUtils
{
    public static async Task<PageTest.TestResult> RenderAsync(string path)
    {
        var services = new ServiceCollection();

        services.AddWebFormsCore(b => { b.AddControlCompiler(); });

        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, PageTest.TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options => options.Enabled = false);

        services.AddOptions<WebFormsCoreOptions>()
            .Configure(options =>
            {
                options.AddWebFormsCoreScript = false;
                options.AddWebFormsCoreHeadScript = false;
                options.EnableWebFormsPolyfill = false;
            });

        var serviceProvider = services.BuildServiceProvider();
        await using var stream = new MemoryStream();

        var pageManager = serviceProvider.GetRequiredService<IPageManager>();

        var coreRequest = Substitute.For<HttpRequest>();
        coreRequest.Method.Returns("GET");

        var coreResponse = Substitute.For<HttpResponse>();
        var responseHeaders = new HeaderDictionary();
        coreResponse.Headers.Returns(responseHeaders);
        coreResponse.Body.Returns(stream);

        var coreContext = Substitute.For<HttpContext>();
        coreContext.Request.Returns(coreRequest);
        coreContext.Response.Returns(coreResponse);
        coreContext.RequestServices.Returns(serviceProvider);

        var page = await pageManager.RenderPageAsync(coreContext, path);

        stream.Position = 0;

        var html = Encoding.UTF8.GetString(stream.ToArray());

        return new PageTest.TestResult(serviceProvider, page, html);
    }
}