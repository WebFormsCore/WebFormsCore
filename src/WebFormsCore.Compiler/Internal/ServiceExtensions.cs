using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public static class HostingServiceExtensions
{
    public static IServiceCollection AddWebFormsControlCompiler(this IServiceCollection services)
    {
        services.AddHostedService<InitializeViewManager>();
        services.AddSingleton<IControlManager, ControlManager>();
        return services;
    }
}
