using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

using AddMiddleware = Action<Func<
    Func<IDictionary<string, object>, Task>,
    Func<IDictionary<string, object>, Task>
>>;

public static class OwinServiceExtensions
{
    public static IServiceCollection UseOwinWebForms(this IServiceCollection services)
    {
        services.AddWebFormsInternals();
        services.AddLogging();
        services.AddSingleton<IWebFormsEnvironment, WebFormsEnvironment>();
        return services;
    }

    public static AddMiddleware UseWebForms(this AddMiddleware pipeline, Action<IServiceCollection> configureServices = null)
    {
        pipeline(next =>
        {
            var services = new ServiceCollection();
            services.UseOwinWebForms();
            configureServices?.Invoke(services);

            var middleware = new WebFormsCoreMiddleware(next, services);
            return middleware.Invoke;
        });

        return pipeline;
    }
}
