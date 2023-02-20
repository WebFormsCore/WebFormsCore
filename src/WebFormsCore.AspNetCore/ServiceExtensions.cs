using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Middlewares;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IApplicationBuilder UseWebForms(this IApplicationBuilder app)
    {
        app.UseMiddleware<PageMiddleware>();

        return app;
    }

    private static RequestDelegate CreatePathDelegate(string path)
    {
        return context =>
        {
            var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();
            return application.ProcessAsync(context, path, context.RequestServices, context.RequestAborted);
        };
    }

    public static IEndpointConventionBuilder MapAspx(this IEndpointRouteBuilder app, string path)
    {
        return app.Map(path, CreatePathDelegate(path));
    }

    public static IEndpointConventionBuilder MapAspx(this IEndpointRouteBuilder app, string pattern, string path)
    {
        return app.Map(pattern, CreatePathDelegate(path));
    }

    public static IEndpointConventionBuilder MapAspx(this IEndpointRouteBuilder app, RoutePattern pattern, string path)
    {
        return app.Map(pattern, CreatePathDelegate(path));
    }

    public static IEndpointConventionBuilder MapFallbackToAspx(this IEndpointRouteBuilder app)
    {
        Task RequestDelegate(HttpContext context)
        {
            var application = context.RequestServices.GetRequiredService<IWebFormsApplication>();
            var path = application.GetPath(context);

            if (path == null)
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            return application.ProcessAsync(context, path, context.RequestServices, context.RequestAborted);
        }

        return app.MapFallback("{*path}", RequestDelegate);
    }

    public static IServiceCollection AddWebForms(this IServiceCollection services)
    {
        services.AddWebFormsInternals();
        services.AddSingleton<IWebFormsEnvironment, WebFormsEnvironment>();
        return services;
    }
}
