using System;
using System.Threading.Tasks;
using HttpStack;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class HttpStackExtensions
{
    public static IHttpStackBuilder UseWebFormsCore(this IHttpStackBuilder builder)
    {
        builder.Use((context, next) =>
        {
            var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();
            var path = application.GetPath(context.Request.Path);

            return path == null ? next() : application.ProcessAsync(context, path, context.RequestAborted);
        });

        return builder;
    }

    public static async ValueTask<Page?> ExecutePageAsync(this IHttpContext context, string? path = null, bool throwOnError = false)
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

    public static async ValueTask<T> ExecutePageAsync<T>(this IHttpContext context)
        where T : Page
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return (T) await application.RenderPageAsync(context, typeof(T), context.RequestAborted);
    }

    public static async ValueTask<Page> ExecutePageAsync(this IHttpContext context, Type page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return await application.RenderPageAsync(context, page, context.RequestAborted);
    }

    public static async ValueTask ExecutePageAsync(this IHttpContext context, Page page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        await application.RenderPageAsync(context, page, context.RequestAborted);
    }
}
