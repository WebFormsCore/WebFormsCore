using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI;

namespace WebFormsCore;

public static class AspNetCoreExtensions
{
    public static IApplicationBuilder UseWebFormsCore(this IApplicationBuilder builder)
    {
        // Auto-discover assemblies with embedded static files and register file providers
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var attribute = assembly.GetCustomAttribute<WebFormsStaticFilesAttribute>();
            if (attribute == null)
            {
                continue;
            }

            ManifestEmbeddedFileProvider fileProvider;
            try
            {
                fileProvider = new ManifestEmbeddedFileProvider(assembly, attribute.Root);
            }
            catch (InvalidOperationException)
            {
                // Manifest not embedded in assembly — skip static file serving for this assembly
                continue;
            }

            builder.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = fileProvider,
                RequestPath = ""
            });
        }

        // Mark that WebFormsCore middleware is registered so controls can use script links
        builder.Use(async (context, next) =>
        {
            context.Items["RegisteredWebFormsCore"] = null;
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

        var builder = endpoints.Map(pattern, async context =>
        {
            await ExecutePageAsync<T>(context);
        });

        ApplyPageMetadata(builder, typeof(T));
        return builder;
    }

    public static IEndpointConventionBuilder MapPage(this IEndpointRouteBuilder endpoints, string pattern, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type page)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(page);

        var builder = endpoints.Map(pattern, async context =>
        {
            await ExecutePageAsync(context, page);
        });

        ApplyPageMetadata(builder, page);
        return builder;
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

            var builder = endpoints.Map(pattern, async context =>
            {
                await ExecutePageAsync(context, pageType);
            });

            ApplyPageMetadata(builder, pageType);
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

    public static IEndpointConventionBuilder MapControl<T>(this IEndpointRouteBuilder endpoints, string pattern, Func<T> controlFactory)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(controlFactory);

        return endpoints.Map(pattern, context => ExecuteControlAsync(context, new FuncRefControl(c =>
        {
            c.Controls.AddWithoutPageEvents(controlFactory());
            return Task.CompletedTask;
        })));
    }

    public static IEndpointConventionBuilder MapControl<T>(this IEndpointRouteBuilder endpoints, string pattern, Func<IServiceProvider, T> controlFactory)
        where T : Control
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(controlFactory);

        return endpoints.Map(pattern, context => ExecuteControlAsync(context, new FuncRefControl(c =>
        {
            c.Controls.AddWithoutPageEvents(controlFactory(context.RequestServices));
            return Task.CompletedTask;
        })));
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

        var pageFactory = context.RequestServices.GetRequiredService<IPageFactory>();

        await ExecutePageAsync(context, await pageFactory.CreatePageForControlAsync(context, control));
    }

    private static void ApplyPageMetadata(IEndpointConventionBuilder builder, Type pageType)
    {
        var attributes = pageType.GetCustomAttributes(true);

        var authorizeData = attributes.OfType<IAuthorizeData>().ToArray();
        if (authorizeData.Length > 0)
        {
            builder.RequireAuthorization(authorizeData);
        }

        if (attributes.OfType<IAllowAnonymous>().Any())
        {
            builder.AllowAnonymous();
        }

        foreach (var attribute in attributes)
        {
            if (attribute is IAuthorizeData or IAllowAnonymous)
            {
                continue;
            }

            builder.WithMetadata(attribute);
        }
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
