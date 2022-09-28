using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using WebFormsCore.Internal;
using WebFormsCore.Serializer;
using WebFormsCore.UI;
using WebFormsCore.UI.Attributes;
using WebFormsCore.UI.HtmlControls;
using WebFormsCore.UI.WebControls;

namespace WebFormsCore;

public static class ServiceExtensions
{
    public static IServiceCollection AddWebFormsInternals(this IServiceCollection services)
    {
        services.AddHostedService<InitializeViewManager>();
        services.AddSingleton<IViewStateManager, ViewStateManager>();

        services.AddSingleton(typeof(IControlFactory<>), typeof(ControlFactory<>));
        services.AddSingleton<ViewManager>();
        services.AddSingleton<IWebFormsApplication, WebFormsApplications>();
        services.AddScoped<IWebObjectActivator, WebObjectActivator>();

        services.AddPooledControl<LiteralControl>();
        services.AddPooledControl<LiteralHtmlControl>();

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
        services.AddViewStateSerializer<TextBoxMode>();
        services.AddViewStateSerializer<Unit>();
        services.AddViewStateSerializer<AutoCompleteType>();

        services.AddSingleton<IAttributeParser<string>, StringAttributeParser>();
        services.AddSingleton<IAttributeParser<int>, Int32AttributeParser>();
        services.AddSingleton<IAttributeParser<bool>, BoolAttributeParser>();

        return services;
    }

    public static IServiceCollection AddPooledControl<T>(this IServiceCollection services, int maxAmount = 1024)
        where T : Control, new()
    {
        if (typeof(T).GetCustomAttributes<ViewPathAttribute>().Any())
        {
            throw new InvalidOperationException("Cannot pool a control with a view path");
        }

        services.AddSingleton<ObjectPool<T>>(
            new DefaultObjectPool<T>(new ControlObjectPolicy<T>(), maxAmount)
        );

        services.AddScoped<IControlFactory<T>, PooledControlFactory<T>>();

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
        services.AddSingleton<IViewStateSerializer>(p => p.GetRequiredService<IViewStateSerializer<T>>());
        services.AddSingleton(new ViewStateSerializerRegistration(offset, typeof(T), typeof(IViewStateSerializer<T>)));

        return services;
    }
}
