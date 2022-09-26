using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using WebFormsCore.Middlewares;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IApplicationBuilder UseWebForms(this IApplicationBuilder app)
    {
        app.UseSystemWebAdapters();
        app.UseMiddleware<PageMiddleware>();

        return app;
    }

    public static IServiceCollection AddWebForms(this IServiceCollection services)
    {
        services.AddSystemWebAdapters();
        services.AddWebFormsCore();
        services.AddSingleton<IWebFormsEnvironment, WebFormsEnvironment>();
        return services;
    }
}
