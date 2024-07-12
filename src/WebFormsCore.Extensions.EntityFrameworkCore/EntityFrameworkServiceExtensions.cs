using Microsoft.Extensions.DependencyInjection;
using WebFormsCore.Providers;

namespace WebFormsCore;

public static class EntityFrameworkServiceExtensions
{
    public static IWebFormsCoreBuilder AddEntityFrameworkCore(this IWebFormsCoreBuilder builder)
    {
        builder.Services.AddSingleton<IQueryableProvider, EntityFrameworkCoreQueryableProvider>();
        return builder;
    }
}