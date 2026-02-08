using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.UI.HtmlControls;
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

    public static IEndpointConventionBuilder MapPage(this IEndpointRouteBuilder endpoints, string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.Map(pattern, async context =>
        {
            await ExecutePageAsync(context, pattern, true);
        });
    }

    public static IEndpointConventionBuilder MapPage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : Page
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.Map(pattern, async context =>
        {
            await ExecutePageAsync<T>(context);
        });
    }

    public static IEndpointConventionBuilder MapPage(this IEndpointRouteBuilder endpoints, string pattern, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(page);

        return endpoints.Map(pattern, async context =>
        {
            await ExecutePageAsync(context, page);
        });
    }

    /// <summary>
    /// Discovers all pages with <c>EndPoint</c> directives and maps them as ASP.NET Core endpoints.
    /// Pages declare their endpoint pattern in the .aspx file:
    /// <code>&lt;%@ Page EndPoint="/edit/{Id:int}" %&gt;</code>
    /// </summary>
    public static IEndpointRouteBuilder MapPages(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var visited = new HashSet<Assembly>();
        var entry = Assembly.GetEntryAssembly();

        if (entry != null)
        {
            MapPagesFromAssemblies(endpoints, entry, visited);
        }

        return endpoints;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Assembly references are needed for endpoint discovery")]
    private static void MapPagesFromAssemblies(IEndpointRouteBuilder endpoints, Assembly assembly, HashSet<Assembly> visited)
    {
        if (!visited.Add(assembly))
        {
            return;
        }

        MapPagesFromAssembly(endpoints, assembly);

        var loaded = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(a => a.GetName().FullName);

        foreach (var referencedName in assembly.GetReferencedAssemblies())
        {
            if (loaded.TryGetValue(referencedName.FullName, out var referenced))
            {
                MapPagesFromAssemblies(endpoints, referenced, visited);
            }
        }
    }

    internal static void MapPagesFromAssembly(IEndpointRouteBuilder endpoints, Assembly assembly)
    {
        foreach (var attribute in assembly.GetCustomAttributes<AssemblyRouteAttribute>())
        {
            var pattern = attribute.Pattern;
            var pageType = attribute.PageType;

            endpoints.Map(pattern, async context =>
            {
                await ExecutePageAsync(context, pageType);
            });
        }
    }

    public static IEndpointConventionBuilder MapControl<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TControl>(this IEndpointRouteBuilder endpoints, string pattern)
        where TControl : Control
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.Map(pattern, context =>
        {
            var activator = context.RequestServices.GetRequiredService<IWebObjectActivator>();
            var control = activator.CreateControl<TControl>();
            return ExecuteControlAsync(context, control);
        });
    }

    public static IEndpointConventionBuilder MapControl(this IEndpointRouteBuilder endpoints, string pattern, Func<Control> controlFactory)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(controlFactory);

        return endpoints.Map(pattern, context => ExecuteControlAsync(context, controlFactory()));
    }

    public static IEndpointConventionBuilder MapControl(this IEndpointRouteBuilder endpoints, string pattern, Func<IServiceProvider, Control> controlFactory)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(controlFactory);

        return endpoints.Map(pattern, context => ExecuteControlAsync(context, controlFactory(context.RequestServices)));
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

    private static async Task ExecuteControlAsync(HttpContext context, Control control)
    {
        if (control is Page page)
        {
            await ExecutePageAsync(context, page);
            return;
        }

        var activator = context.RequestServices.GetRequiredService<IWebObjectActivator>();
        page = activator.CreateControl<Page>();

        var doctype = activator.CreateLiteral("<!DOCTYPE html>");
        page.Controls.AddWithoutPageEvents(doctype);
        page.Controls.AddWithoutPageEvents(activator.CreateControl<HtmlHead>());

        var body = activator.CreateControl<HtmlBody>();
        body.Controls.AddWithoutPageEvents(control);
        page.Controls.AddWithoutPageEvents(body);

        await ExecutePageAsync(context, page);
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
