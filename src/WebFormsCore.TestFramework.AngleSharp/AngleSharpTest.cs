using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Tests;
using WebFormsCore.UI;

namespace WebFormsCore.TestFramework.AngleSharp;

public static class AngleSharpTest
{
    public static Task<ITestContext<T>> RenderAsync<T, TTypeProvider>(bool enableViewState = true)
        where T : Page
        where TTypeProvider : class, IControlTypeProvider
    {
        return RenderAsync<T, TTypeProvider>(async (pageManager, context) => (T) await pageManager.RenderPageAsync(context, typeof(T)), enableViewState);
    }

    public static Task<ITestContext<Page>> RenderAsync<TTypeProvider>(string path, bool enableViewState = true)
        where TTypeProvider : class, IControlTypeProvider
    {
        return RenderAsync<Page, TTypeProvider>(async (pageManager, context) => await pageManager.RenderPageAsync(context, path), enableViewState);
    }

    private static async Task<ITestContext<T>> RenderAsync<T, TTypeProvider>(
        Func<IPageManager, HttpContext, Task<T>> create,
        bool enableViewState)
        where T : Page
        where TTypeProvider : class, IControlTypeProvider
    {
        var services = new ServiceCollection();

        services.AddWebFormsCore(builder =>
        {
            builder.Services.AddSingleton<IControlTypeProvider, TTypeProvider>();
        });

        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, TestEnvironment>();

        services.AddOptions<ViewStateOptions>()
            .Configure(options =>
            {
                options.Enabled = enableViewState;
            });

        services.AddOptions<WebFormsCoreOptions>()
            .Configure(options =>
            {
                options.AddWebFormsCoreScript = false;
                options.AddWebFormsCoreHeadScript = false;
                options.EnableWebFormsPolyfill = false;
            });

        var serviceProvider = services.BuildServiceProvider();

        var result = new AngleSharpTestContext<T>(serviceProvider, create);
        await result.GetAsync();
        return result;
    }
}
