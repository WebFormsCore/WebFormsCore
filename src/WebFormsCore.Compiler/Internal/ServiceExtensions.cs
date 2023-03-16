using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public static class HostingServiceExtensions
{
    public static IServiceCollection AddWebFormsHosting(this IServiceCollection services)
    {
        services.AddHostedService<InitializeViewManager>();
        services.AddSingleton<IControlManager, ControlManager>();
        return services;
    }
}
