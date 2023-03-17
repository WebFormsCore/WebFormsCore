using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebForms(this IServiceCollection services)
    {
        services.AddWebFormsInternals();
        services.AddSingleton<IWebFormsEnvironment, WebFormsEnvironment>();
        return services;
    }
}
