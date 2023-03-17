using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Implementation;
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
            var contextImpl = new HttpContextImpl(); // TODO: Pooling
            contextImpl.SetHttpContext(context);

            return application.ProcessAsync(contextImpl, path, context.RequestServices, context.RequestAborted);
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
            var path = application.GetPath(context.Request.Path);

            if (path == null)
            {
                context.Response.StatusCode = 404;
                return Task.CompletedTask;
            }

            var contextImpl = new HttpContextImpl(); // TODO: Pooling
            contextImpl.SetHttpContext(context);

            return application.ProcessAsync(contextImpl, path, context.RequestServices, context.RequestAborted);
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
