using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

public static class HostingServiceExtensions
{
    public static IWebFormsCoreBuilder AddControlCompiler(this IWebFormsCoreBuilder builder)
    {
        builder.Services.AddHostedService<InitializeViewManager>();
        builder.Services.AddSingleton<IControlManager, ControlManager>();
        return builder;
    }
}
