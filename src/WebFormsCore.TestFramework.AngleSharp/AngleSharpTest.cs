using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Tests;
using WebFormsCore.UI;

namespace WebFormsCore.TestFramework.AngleSharp;

public static class AngleSharpTest
{
    public static Task<ITestContext<T>> RenderAsync<T>(bool enableViewState = true)
        where T : Page
    {
        return RenderAsync<T>(async (pageManager, context) => (T) await pageManager.RenderPageAsync(context, typeof(T)), enableViewState);
    }

    public static Task<ITestContext<Page>> RenderAsync<TTypeProvider>(string path, bool enableViewState = true)
        where TTypeProvider : class, IControlTypeProvider
    {
        return RenderAsync<Page>(async (pageManager, context) => await pageManager.RenderPageAsync(context, path), enableViewState, typeof(TTypeProvider));
    }

    private static async Task<ITestContext<T>> RenderAsync<T>(
        Func<IPageManager, HttpContext, Task<T>> create,
        bool enableViewState,
        Type? typeProvider = null)
        where T : Page
    {
        var services = new ServiceCollection();

        services.AddWebFormsCore(builder =>
        {
            typeProvider ??= typeof(T).Assembly.GetCustomAttribute<AssemblyControlTypeProviderAttribute>()?.Type;

            if (typeProvider is null)
            {
                throw new InvalidOperationException("Type provider not found");
            }

            builder.Services.AddSingleton(typeof(IControlTypeProvider), typeProvider);
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
