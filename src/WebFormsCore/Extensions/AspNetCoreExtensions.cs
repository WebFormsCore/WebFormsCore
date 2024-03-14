using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class AspNetCoreExtensions
{
    public static IApplicationBuilder UseWebFormsCore(this IApplicationBuilder builder)
    {
        builder.Use(async (context, next) =>
        {
            context.Items["RegisteredWebFormsCore"] = null;

            if (StaticFiles.Files.TryGetValue(context.Request.Path, out var content))
            {
                context.Response.ContentType = "application/javascript";
                await context.Response.WriteAsync(content);
                return;
            }

            await next();
        });

        return builder;
    }

    public static IApplicationBuilder UsePage(this IApplicationBuilder builder)
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

    public static async Task<Page?> ExecutePageAsync(this HttpContext context, string? path = null, bool throwOnError = false)
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

    public static async Task<T> ExecutePageAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this HttpContext context)
        where T : Page
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return (T) await application.RenderPageAsync(context, typeof(T), context.RequestAborted);
    }

    public static async Task<Page> ExecutePageAsync(this HttpContext context, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        return await application.RenderPageAsync(context, page, context.RequestAborted);
    }

    public static async Task ExecutePageAsync(this HttpContext context, Page page)
    {
        var application = context.RequestServices.GetRequiredService<IPageManager>();

        await application.RenderPageAsync(context, page, context.RequestAborted);
    }

    public static void RunPage(this IApplicationBuilder builder, string path)
    {
        builder.Run(async context =>
        {
            await ExecutePageAsync(context, path, true);
        });
    }

    public static void RunPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IApplicationBuilder builder)
        where T : Page
    {
        builder.Run(async context =>
        {
            await ExecutePageAsync<T>(context);
        });
    }

    public static void RunPage(this IApplicationBuilder builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        builder.Run(async context =>
        {
            await ExecutePageAsync(context, page);
        });
    }
}
