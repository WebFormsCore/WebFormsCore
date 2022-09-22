using System.Web.Serializer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebForms.AspNetCore;
using WebForms.AspNetCore.Middlewares;

namespace System.Web;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebForms(this IServiceCollection services)
    {
        services.AddSystemWebAdapters();
        services.AddScoped<IWebObjectActivator, WebObjectActivator>();
        services.AddSingleton<ObjectPool<LiteralControl>>(
            new DefaultObjectPool<LiteralControl>(new ControlObjectPolicy<LiteralControl>())
        );

        services.AddSingleton<ObjectPool<HtmlGenericControl>>(
            new DefaultObjectPool<HtmlGenericControl>(new ControlObjectPolicy<HtmlGenericControl>())
        );

        services.AddSingleton<IViewStateSerializer<object?>, ObjectViewStateSerializer>();
        services.AddViewStateSerializer<string?, StringViewStateSerializer>();
        services.AddViewStateSerializer<int>();
        services.AddViewStateSerializer<uint>();
        services.AddViewStateSerializer<short>();
        services.AddViewStateSerializer<ushort>();
        services.AddViewStateSerializer<byte>();
        services.AddViewStateSerializer<sbyte>();
        services.AddViewStateSerializer<long>();
        services.AddViewStateSerializer<ulong>();
        services.AddViewStateSerializer<float>();
        services.AddViewStateSerializer<double>();
        services.AddViewStateSerializer<decimal>();
        services.AddViewStateSerializer<bool>();
        services.AddViewStateSerializer<char>();
        services.AddViewStateSerializer<DateTime>();
        services.AddViewStateSerializer<DateTimeOffset>();

        return services;
    }

    public static IServiceCollection AddViewStateSerializer<T>(this IServiceCollection services)
        where T : struct
    {
        AddViewStateSerializer<T, MarshalViewStateSerializer<T>>(services);
        return services;
    }

    public static IServiceCollection AddViewStateSerializer<T, TSerializer>(this IServiceCollection services)
        where TSerializer : class, IViewStateSerializer<T>
    {
        var offset = (byte)(1 + services.Count(i => i.ServiceType == typeof(ViewStateSerializerRegistration)));
        
        services.AddSingleton<IViewStateSerializer<T>, TSerializer>();
        services.AddSingleton(new ViewStateSerializerRegistration(offset, typeof(T), typeof(IViewStateSerializer<T>)));

        return services;
    }

    public static IApplicationBuilder UseWebForms(this IApplicationBuilder app)
    {
        app.UseSystemWebAdapters();
        app.UseMiddleware<PageMiddleware>();

        return app;
    }
}
