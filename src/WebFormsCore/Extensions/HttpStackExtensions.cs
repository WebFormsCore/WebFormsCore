using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class HttpStackExtensions
{
    public static IHttpStackBuilder UsePage(this IHttpStackBuilder builder)
    {
        builder.Use((context, next) =>
        {
            var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();
            var path = application.GetPath(context.Request.Path);

            return path == null || !path.EndsWith(".aspx", StringComparison.OrdinalIgnoreCase)
                ? next()
                : application.ProcessAsync(context, path, context.RequestAborted);
        });

        return builder;
    }

    public static async Task<Page?> ExecutePageAsync(this IHttpContext context, string? path = null, bool throwOnError = false)
    {
        var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();

        path ??= context.Request.Path;

        var pagePath = application.GetPath(path);

        if (pagePath == null)
        {
            if (throwOnError)
            {
                throw new InvalidOperationException($"No page found for path '{path}'.");
            }

            return null;
        }

        return await application.ProcessAsync(context, pagePath, context.RequestAborted);
    }

    public static async Task<T> ExecutePageAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IHttpContext context)
        where T : Page
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return (T) await application.RenderPageAsync(context, typeof(T), context.RequestAborted);
    }

    public static async Task<Page> ExecutePageAsync(this IHttpContext context, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return await application.RenderPageAsync(context, page, context.RequestAborted);
    }

    public static async Task ExecutePageAsync(this IHttpContext context, Page page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        await application.RenderPageAsync(context, page, context.RequestAborted);
    }

    public static void RunPage(this IHttpStackBuilder builder, string path)
    {
        builder.Run(context => ExecutePageAsync(context, path, true));
    }

    public static void RunPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IHttpStackBuilder builder)
        where T : Page
    {
        builder.Run(ExecutePageAsync<T>);
    }

    public static void RunPage(this IHttpStackBuilder builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        builder.Run(context => ExecutePageAsync(context, page));
    }
}
