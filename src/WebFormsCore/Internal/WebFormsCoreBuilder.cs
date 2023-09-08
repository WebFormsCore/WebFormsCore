using Microsoft.Extensions.DependencyInjection;

namespace WebFormsCore;

internal class WebFormsCoreBuilder : IWebFormsCoreBuilder
{
    public WebFormsCoreBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}