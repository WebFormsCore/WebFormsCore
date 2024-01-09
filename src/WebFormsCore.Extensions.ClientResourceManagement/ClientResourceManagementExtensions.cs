using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebFormsCore.Events;
using WebFormsCore.Features;
using WebFormsCore.Providers;
using WebFormsCore.Services;
using WebFormsCore.UI;
using WebFormsCore.UI.Attributes;

namespace WebFormsCore;

public class ClientResourceManagementBuilder
{
    public ClientResourceManagementBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }
}

public static class ClientResourceManagementExtensions
{
    public static ClientResourceManagementBuilder AddClientResourceManagement(this IWebFormsCoreBuilder builder)
    {
        builder.Services.TryAddSingleton<IPageService, ClientResourceManagementService>();
        builder.Services.TryAddScoped<IClientDependencyCollection, ClientDependencyCollection>();
        builder.TryAddEnumAttributeParser<CssMediaType>();
        return new ClientResourceManagementBuilder(builder.Services);
    }

    public static IWebFormsCoreBuilder AddClientResourceManagement(this IWebFormsCoreBuilder builder, Action<ClientResourceManagementBuilder> configure)
    {
        var clientResourceManagementBuilder = builder.AddClientResourceManagement();
        configure(clientResourceManagementBuilder);
        return builder;
    }

    public static ClientResourceManagementBuilder RegisterPrefix(this ClientResourceManagementBuilder builder, string alias, Func<Page, string> getter)
    {
        builder.Services.AddSingleton<IClientDependencyPathProvider>(new PrefixClientDependencyPathProvider<Page>(alias, getter));
        return builder;
    }

    public static ClientResourceManagementBuilder RegisterPrefix<TPage>(this ClientResourceManagementBuilder builder, string alias, Func<TPage, string> getter)
    {
        builder.Services.AddSingleton<IClientDependencyPathProvider>(new PrefixClientDependencyPathProvider<TPage>(alias, getter));
        return builder;
    }

    public static ClientResourceManagementBuilder RegisterPrefix(this ClientResourceManagementBuilder builder, string alias, string path)
    {
        builder.Services.AddSingleton<IClientDependencyPathProvider>(new PrefixClientDependencyPathProvider<Page>(alias, _ => path));
        return builder;
    }
}
